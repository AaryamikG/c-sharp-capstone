using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;

namespace CatalogService.Tests;

public class CatalogServiceDbContextTests
{
    private static CatalogServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogServiceDbContext(options);
    }

    [Fact]
    public async Task AddBook_PersistsAndAutoPopulatesAuditFields()
    {
        await using var context = CreateContext();

        var book = new Book
        {
            BookId = Guid.NewGuid(),
            Isbn = "978-0-13-468599-1",
            Title = "Clean Code",
            Author = "Robert C. Martin",
            Genre = "Technology",
            PublicationYear = 2008,
            TotalCopies = 5,
            AvailableCopies = 2
        };

        context.Books.Add(book);
        await context.SaveChangesAsync();

        var saved = await context.Books.SingleAsync(b => b.BookId == book.BookId);
        Assert.Equal("Clean Code", saved.Title);
        Assert.NotEqual(default, saved.CreatedAt);
        Assert.NotEqual(default, saved.UpdatedAt);
    }

    [Theory]
    [InlineData(2, BookStatus.Available)]
    [InlineData(0, BookStatus.CheckedOut)]
    public void Status_IsComputedFromAvailableCopies(int availableCopies, BookStatus expected)
    {
        var book = new Book { AvailableCopies = availableCopies };
        Assert.Equal(expected, book.Status);
    }
}
