using CatalogService.Dtos;

namespace CatalogService.Services;

public interface IBookService
{
    Task<PagedResult<BookListItemResponse>> SearchAsync(BookQueryParameters parameters);
    Task<BookDetailResponse> GetByIdAsync(Guid bookId);
    Task<UpdateAvailabilityResponse> UpdateAvailabilityAsync(Guid bookId, int delta);
}
