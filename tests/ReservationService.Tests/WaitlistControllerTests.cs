using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Moq;
using ReservationService.Dtos;
using ReservationService.Dtos.External;

namespace ReservationService.Tests;

public class WaitlistControllerTests : IClassFixture<ReservationServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ReservationServiceWebApplicationFactory _factory;

    public WaitlistControllerTests(ReservationServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void AuthorizeAs(Guid userId, string role = "Patron")
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, role));
    }

    [Fact]
    public async Task Join_BookUnavailable_Returns400BookAvailable()
    {
        var bookId = Guid.NewGuid();
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, AvailableCopies = 3 });

        AuthorizeAs(Guid.NewGuid());
        var response = await _client.PostAsJsonAsync("/api/reservations/waitlist", new WaitlistJoinRequest { BookId = bookId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("BOOK_AVAILABLE", error!.Error);
    }

    [Fact]
    public async Task Join_Success_Returns201WithPosition()
    {
        var bookId = Guid.NewGuid();
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, Title = "Refactoring", Author = "Martin Fowler", AvailableCopies = 0 });

        AuthorizeAs(Guid.NewGuid());
        var response = await _client.PostAsJsonAsync("/api/reservations/waitlist", new WaitlistJoinRequest { BookId = bookId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WaitlistJoinResponse>(TestJson.Options);
        Assert.Equal(1, body!.Position);
    }

    [Fact]
    public async Task JoinTwice_SecondCallReturns400AlreadyWaitlisted()
    {
        var bookId = Guid.NewGuid();
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, Title = "T", Author = "A", AvailableCopies = 0 });

        AuthorizeAs(Guid.NewGuid());
        await _client.PostAsJsonAsync("/api/reservations/waitlist", new WaitlistJoinRequest { BookId = bookId });
        var second = await _client.PostAsJsonAsync("/api/reservations/waitlist", new WaitlistJoinRequest { BookId = bookId });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var error = await second.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("ALREADY_WAITLISTED", error!.Error);
    }

    [Fact]
    public async Task GetMine_ReturnsJoinedEntry()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, Title = "T", Author = "A", AvailableCopies = 0 });

        AuthorizeAs(userId);
        await _client.PostAsJsonAsync("/api/reservations/waitlist", new WaitlistJoinRequest { BookId = bookId });

        var response = await _client.GetAsync("/api/reservations/waitlist");
        var body = await response.Content.ReadFromJsonAsync<WaitlistEntriesResponse>(TestJson.Options);

        Assert.Single(body!.Entries);
    }

    [Fact]
    public async Task Cancel_UnknownEntry_Returns404()
    {
        AuthorizeAs(Guid.NewGuid());
        var response = await _client.DeleteAsync($"/api/reservations/waitlist/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEntry_Returns200Cancelled()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, Title = "T", Author = "A", AvailableCopies = 0 });

        AuthorizeAs(userId);
        var joinResponse = await _client.PostAsJsonAsync("/api/reservations/waitlist", new WaitlistJoinRequest { BookId = bookId });
        var joined = await joinResponse.Content.ReadFromJsonAsync<WaitlistJoinResponse>(TestJson.Options);

        var response = await _client.DeleteAsync($"/api/reservations/waitlist/{joined!.WaitlistId}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<WaitlistCancelResponse>(TestJson.Options);
        Assert.Equal(ReservationService.Models.WaitlistStatus.Cancelled, body!.Status);
    }
}
