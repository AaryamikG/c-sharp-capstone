namespace CatalogService.Exceptions;

public class BookNotFoundException(Guid bookId) : Exception($"Book not found with ID: {bookId}");
