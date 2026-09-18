using CatalogService.Data;
using CatalogService.Dtos;
using CatalogService.Exceptions;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Services;

public class BookService : IBookService
{
    private readonly CatalogServiceDbContext _context;

    public BookService(CatalogServiceDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BookListItemResponse>> SearchAsync(BookQueryParameters parameters)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Query))
        {
            var term = parameters.Query.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(term) || b.Author.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Genre))
        {
            var genre = parameters.Genre.ToLower();
            query = query.Where(b => b.Genre.ToLower() == genre);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Isbn))
        {
            var isbn = parameters.Isbn.ToLower();
            query = query.Where(b => b.Isbn.ToLower() == isbn);
        }

        if (parameters.AvailableOnly)
        {
            query = query.Where(b => b.AvailableCopies > 0);
        }

        var descending = string.Equals(parameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        query = parameters.SortBy?.ToLowerInvariant() switch
        {
            "author" => descending ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
            "publicationyear" => descending ? query.OrderByDescending(b => b.PublicationYear) : query.OrderBy(b => b.PublicationYear),
            _ => descending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title)
        };

        var totalElements = await query.CountAsync();
        var page = Math.Max(parameters.Page, 0);
        var size = parameters.Size <= 0 ? 20 : parameters.Size;
        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling(totalElements / (double)size);

        var books = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<BookListItemResponse>
        {
            Content = books.Select(ToListItemResponse).ToList(),
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            Last = page >= totalPages - 1
        };
    }

    public async Task<BookDetailResponse> GetByIdAsync(Guid bookId)
    {
        var book = await _context.Books.SingleOrDefaultAsync(b => b.BookId == bookId)
            ?? throw new BookNotFoundException(bookId);

        return new BookDetailResponse
        {
            BookId = book.BookId,
            Isbn = book.Isbn,
            Title = book.Title,
            Author = book.Author,
            Genre = book.Genre,
            PublicationYear = book.PublicationYear,
            Description = book.Description,
            Publisher = book.Publisher,
            PageCount = book.PageCount,
            Language = book.Language,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            Status = book.Status,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt
        };
    }

    public async Task<UpdateAvailabilityResponse> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        var book = await _context.Books.SingleOrDefaultAsync(b => b.BookId == bookId)
            ?? throw new BookNotFoundException(bookId);

        var newAvailableCopies = book.AvailableCopies + delta;
        if (newAvailableCopies < 0 || newAvailableCopies > book.TotalCopies)
        {
            throw new InvalidAvailabilityAdjustmentException();
        }

        book.AvailableCopies = newAvailableCopies;
        await _context.SaveChangesAsync();

        return new UpdateAvailabilityResponse
        {
            BookId = book.BookId,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            Status = book.Status
        };
    }

    private static BookListItemResponse ToListItemResponse(Book book) => new()
    {
        BookId = book.BookId,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Genre = book.Genre,
        PublicationYear = book.PublicationYear,
        Description = book.Description,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        Status = book.Status
    };
}
