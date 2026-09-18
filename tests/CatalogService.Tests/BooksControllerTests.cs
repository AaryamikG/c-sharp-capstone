using System.Net;
using System.Net.Http.Json;
using CatalogService.Dtos;

namespace CatalogService.Tests;

public class BooksControllerTests : IClassFixture<CatalogServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BooksControllerTests(CatalogServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBooks_Default_ReturnsPagedResultWithSeededBooks()
    {
        var response = await _client.GetAsync("/api/catalog/books");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<BookListItemResponse>>(TestJson.Options);
        Assert.NotNull(result);
        Assert.Equal(0, result!.Page);
        Assert.Equal(20, result.Size);
        Assert.True(result.TotalElements >= 6);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task GetBooks_AvailableOnly_ExcludesZeroAvailability()
    {
        var response = await _client.GetAsync("/api/catalog/books?availableOnly=true");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BookListItemResponse>>(TestJson.Options);

        Assert.All(result!.Content, b => Assert.True(b.AvailableCopies > 0));
    }

    [Fact]
    public async Task GetBooks_QueryFilter_MatchesTitleOrAuthor()
    {
        var response = await _client.GetAsync("/api/catalog/books?query=martin");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BookListItemResponse>>(TestJson.Options);

        Assert.Equal(2, result!.TotalElements);
        Assert.All(result.Content, b => Assert.True(
            b.Title.Contains("martin", StringComparison.OrdinalIgnoreCase) ||
            b.Author.Contains("martin", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task GetBooks_GenreFilter_ReturnsOnlyMatchingGenre()
    {
        var response = await _client.GetAsync("/api/catalog/books?genre=Fiction");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BookListItemResponse>>(TestJson.Options);

        Assert.NotEmpty(result!.Content);
        Assert.All(result.Content, b => Assert.Equal("Fiction", b.Genre));
    }

    [Fact]
    public async Task GetBookById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/catalog/books/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("NOT_FOUND", error!.Error);
    }

    [Fact]
    public async Task GetBookById_KnownId_ReturnsFullDetail()
    {
        var listResponse = await _client.GetAsync("/api/catalog/books?size=1");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<BookListItemResponse>>(TestJson.Options);
        var bookId = list!.Content[0].BookId;

        var response = await _client.GetAsync($"/api/catalog/books/{bookId}");
        response.EnsureSuccessStatusCode();

        var detail = await response.Content.ReadFromJsonAsync<BookDetailResponse>(TestJson.Options);
        Assert.Equal(bookId, detail!.BookId);
        Assert.NotEqual(default, detail.CreatedAt);
    }

    [Fact]
    public async Task UpdateAvailability_Decrement_PersistsChange()
    {
        var listResponse = await _client.GetAsync("/api/catalog/books?availableOnly=true&size=1");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<BookListItemResponse>>(TestJson.Options);
        var book = list!.Content[0];

        var response = await _client.PutAsJsonAsync($"/api/catalog/books/{book.BookId}/availability", new UpdateAvailabilityRequest { Delta = -1 });
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<UpdateAvailabilityResponse>(TestJson.Options);
        Assert.Equal(book.AvailableCopies - 1, updated!.AvailableCopies);
    }

    [Fact]
    public async Task UpdateAvailability_BelowZero_Returns400()
    {
        var listResponse = await _client.GetAsync("/api/catalog/books?size=20");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<BookListItemResponse>>(TestJson.Options);
        var zeroAvailabilityBook = list!.Content.First(b => b.AvailableCopies == 0);

        var response = await _client.PutAsJsonAsync($"/api/catalog/books/{zeroAvailabilityBook.BookId}/availability", new UpdateAvailabilityRequest { Delta = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
