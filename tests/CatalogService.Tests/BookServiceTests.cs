using CatalogService.Data;
using CatalogService.Dtos;
using CatalogService.Exceptions;
using CatalogService.Models;
using CatalogService.Services;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Tests;

public class BookServiceTests
{
    private static CatalogServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogServiceDbContext(options);
    }

    private static Book MakeBook(string title, string author, string genre, int year, int total, int available, string isbn) => new()
    {
        BookId = Guid.NewGuid(),
        Isbn = isbn,
        Title = title,
        Author = author,
        Genre = genre,
        PublicationYear = year,
        TotalCopies = total,
        AvailableCopies = available
    };

    private static async Task<CatalogServiceDbContext> SeedAsync(params Book[] books)
    {
        var context = CreateContext();
        context.Books.AddRange(books);
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task SearchAsync_DefaultParameters_ReturnsFirstPageSortedByTitleAscending()
    {
        await using var context = await SeedAsync(
            MakeBook("Zebra", "A", "G", 2000, 1, 1, "1"),
            MakeBook("Apple", "B", "G", 2001, 1, 1, "2"),
            MakeBook("Mango", "C", "G", 2002, 1, 1, "3"));

        var result = await new BookService(context).SearchAsync(new BookQueryParameters());

        Assert.Equal(3, result.TotalElements);
        Assert.Equal(1, result.TotalPages);
        Assert.True(result.Last);
        Assert.Equal(["Apple", "Mango", "Zebra"], result.Content.Select(b => b.Title));
    }

    [Fact]
    public async Task SearchAsync_Pagination_ReturnsCorrectSlice()
    {
        await using var context = await SeedAsync(
            MakeBook("A", "x", "G", 2000, 1, 1, "1"),
            MakeBook("B", "x", "G", 2000, 1, 1, "2"),
            MakeBook("C", "x", "G", 2000, 1, 1, "3"));

        var result = await new BookService(context).SearchAsync(new BookQueryParameters { Page = 1, Size = 2 });

        Assert.Single(result.Content);
        Assert.Equal("C", result.Content[0].Title);
        Assert.True(result.Last);
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ReturnsEmptyContentNotError()
    {
        await using var context = await SeedAsync(MakeBook("A", "x", "G", 2000, 1, 1, "1"));

        var result = await new BookService(context).SearchAsync(new BookQueryParameters { Query = "nonexistent" });

        Assert.Empty(result.Content);
        Assert.Equal(0, result.TotalElements);
    }

    [Fact]
    public async Task SearchAsync_QueryFilter_MatchesTitleOrAuthor()
    {
        await using var context = await SeedAsync(
            MakeBook("Clean Code", "Robert Martin", "Tech", 2008, 1, 1, "1"),
            MakeBook("Refactoring", "Martin Fowler", "Tech", 2018, 1, 1, "2"),
            MakeBook("1984", "George Orwell", "Fiction", 1949, 1, 1, "3"));

        var result = await new BookService(context).SearchAsync(new BookQueryParameters { Query = "martin" });

        Assert.Equal(2, result.TotalElements);
    }

    [Fact]
    public async Task SearchAsync_GenreFilter_ExactMatch()
    {
        await using var context = await SeedAsync(
            MakeBook("A", "x", "Fiction", 2000, 1, 1, "1"),
            MakeBook("B", "x", "Technology", 2000, 1, 1, "2"));

        var result = await new BookService(context).SearchAsync(new BookQueryParameters { Genre = "Fiction" });

        Assert.Single(result.Content);
        Assert.Equal("A", result.Content[0].Title);
    }

    [Fact]
    public async Task SearchAsync_AvailableOnlyFilter_ExcludesZeroAvailability()
    {
        await using var context = await SeedAsync(
            MakeBook("A", "x", "G", 2000, 1, 0, "1"),
            MakeBook("B", "x", "G", 2000, 1, 2, "2"));

        var result = await new BookService(context).SearchAsync(new BookQueryParameters { AvailableOnly = true });

        Assert.Single(result.Content);
        Assert.Equal("B", result.Content[0].Title);
    }

    [Fact]
    public async Task SearchAsync_SortByPublicationYearDescending()
    {
        await using var context = await SeedAsync(
            MakeBook("A", "x", "G", 2000, 1, 1, "1"),
            MakeBook("B", "x", "G", 2010, 1, 1, "2"),
            MakeBook("C", "x", "G", 1990, 1, 1, "3"));

        var result = await new BookService(context).SearchAsync(new BookQueryParameters { SortBy = "publicationYear", SortOrder = "desc" });

        Assert.Equal(["B", "A", "C"], result.Content.Select(b => b.Title));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ThrowsBookNotFoundException()
    {
        await using var context = CreateContext();
        await Assert.ThrowsAsync<BookNotFoundException>(() => new BookService(context).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_KnownId_ReturnsFullDetail()
    {
        var book = MakeBook("Clean Code", "Robert Martin", "Tech", 2008, 5, 2, "isbn-1");
        book.Publisher = "Prentice Hall";
        await using var context = await SeedAsync(book);

        var result = await new BookService(context).GetByIdAsync(book.BookId);

        Assert.Equal("Clean Code", result.Title);
        Assert.Equal("Prentice Hall", result.Publisher);
        Assert.Equal(BookStatus.Available, result.Status);
    }

    [Theory]
    [InlineData(5, 2, -1, 1, BookStatus.Available)]
    [InlineData(5, 1, -1, 0, BookStatus.CheckedOut)]
    [InlineData(5, 0, 1, 1, BookStatus.Available)]
    public async Task UpdateAvailabilityAsync_AppliesDelta(int total, int available, int delta, int expectedAvailable, BookStatus expectedStatus)
    {
        var book = MakeBook("A", "x", "G", 2000, total, available, "1");
        await using var context = await SeedAsync(book);

        var result = await new BookService(context).UpdateAvailabilityAsync(book.BookId, delta);

        Assert.Equal(expectedAvailable, result.AvailableCopies);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_DecrementBelowZero_Throws()
    {
        var book = MakeBook("A", "x", "G", 5, 0, 0, "1");
        await using var context = await SeedAsync(book);

        await Assert.ThrowsAsync<InvalidAvailabilityAdjustmentException>(
            () => new BookService(context).UpdateAvailabilityAsync(book.BookId, -1));
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_IncrementAboveTotal_Throws()
    {
        var book = MakeBook("A", "x", "G", 5, 5, 5, "1");
        await using var context = await SeedAsync(book);

        await Assert.ThrowsAsync<InvalidAvailabilityAdjustmentException>(
            () => new BookService(context).UpdateAvailabilityAsync(book.BookId, 1));
    }
}
