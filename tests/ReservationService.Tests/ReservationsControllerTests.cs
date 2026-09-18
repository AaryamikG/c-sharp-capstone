using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Moq;
using ReservationService.Dtos;
using ReservationService.Dtos.External;
using ReservationService.Models;

namespace ReservationService.Tests;

public class ReservationsControllerTests : IClassFixture<ReservationServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ReservationServiceWebApplicationFactory _factory;

    public ReservationsControllerTests(ReservationServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void AuthorizeAs(Guid userId, string role)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, role));
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest { BookId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Success_Returns201()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _factory.UserServiceClientMock.Setup(c => c.ValidateAsync(userId))
            .ReturnsAsync(new UserValidationResult { UserId = userId, ActiveReservationsCount = 0 });
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, Title = "Clean Code", Author = "Robert Martin", AvailableCopies = 2 });
        _factory.CatalogServiceClientMock.Setup(c => c.UpdateAvailabilityAsync(bookId, -1))
            .ReturnsAsync(new UpdateAvailabilityResult { BookId = bookId, AvailableCopies = 1 });

        AuthorizeAs(userId, "Patron");
        var response = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest { BookId = bookId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReservationResponse>(TestJson.Options);
        Assert.Equal(ReservationStatus.Reserved, body!.Status);
    }

    [Fact]
    public async Task Create_ReservationLimitExceeded_Returns400()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _factory.UserServiceClientMock.Setup(c => c.ValidateAsync(userId))
            .ReturnsAsync(new UserValidationResult { UserId = userId, ActiveReservationsCount = 5 });

        AuthorizeAs(userId, "Patron");
        var response = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest { BookId = bookId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("RESERVATION_LIMIT_EXCEEDED", error!.Error);
    }

    [Fact]
    public async Task Create_BookUnavailable_Returns400()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _factory.UserServiceClientMock.Setup(c => c.ValidateAsync(userId))
            .ReturnsAsync(new UserValidationResult { UserId = userId, ActiveReservationsCount = 0 });
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, AvailableCopies = 0 });

        AuthorizeAs(userId, "Patron");
        var response = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest { BookId = bookId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("BOOK_UNAVAILABLE", error!.Error);
    }

    [Fact]
    public async Task Checkout_AsPatron_Returns403()
    {
        var userId = Guid.NewGuid();
        AuthorizeAs(userId, "Patron");

        var response = await _client.PostAsJsonAsync($"/api/reservations/{Guid.NewGuid()}/checkout", new CheckoutRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CheckoutThenReturn_AsLibrarian_GoldenPath()
    {
        var patronId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _factory.UserServiceClientMock.Setup(c => c.ValidateAsync(patronId))
            .ReturnsAsync(new UserValidationResult { UserId = patronId, ActiveReservationsCount = 0 });
        _factory.CatalogServiceClientMock.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookInfoResult { BookId = bookId, Title = "Clean Code", Author = "Robert Martin", AvailableCopies = 2 });
        _factory.CatalogServiceClientMock.Setup(c => c.UpdateAvailabilityAsync(bookId, It.IsAny<int>()))
            .ReturnsAsync(new UpdateAvailabilityResult { BookId = bookId, AvailableCopies = 1 });

        AuthorizeAs(patronId, "Patron");
        var createResponse = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest { BookId = bookId });
        var created = await createResponse.Content.ReadFromJsonAsync<ReservationResponse>(TestJson.Options);

        AuthorizeAs(Guid.NewGuid(), "Librarian");
        var checkoutResponse = await _client.PostAsJsonAsync($"/api/reservations/{created!.ReservationId}/checkout", new CheckoutRequest { Notes = "Good" });
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);

        var returnResponse = await _client.PostAsJsonAsync(
            $"/api/reservations/{created.ReservationId}/return",
            new ReturnRequest { Condition = BookCondition.Good, Notes = "Returned fine" });
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);
        var returnBody = await returnResponse.Content.ReadFromJsonAsync<ReturnResponse>();
        Assert.Equal(0, returnBody!.LateDays);
    }

    [Fact]
    public async Task GetHistory_ReturnsPagedResult()
    {
        var userId = Guid.NewGuid();
        AuthorizeAs(userId, "Patron");

        var response = await _client.GetAsync("/api/reservations/history");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<HistoryItem>>(TestJson.Options);
        Assert.NotNull(result);
        Assert.Equal(0, result!.Page);
    }

    [Fact]
    public async Task GetStatistics_NoAuthRequired_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/reservations/statistics/{Guid.NewGuid()}");
        response.EnsureSuccessStatusCode();
    }
}
