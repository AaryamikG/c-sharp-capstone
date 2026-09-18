using CatalogService.Dtos;
using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookListItemResponse>>> Search([FromQuery] BookQueryParameters parameters)
    {
        var result = await _bookService.SearchAsync(parameters);
        return Ok(result);
    }

    [HttpGet("{bookId:guid}")]
    public async Task<ActionResult<BookDetailResponse>> GetById(Guid bookId)
    {
        var result = await _bookService.GetByIdAsync(bookId);
        return Ok(result);
    }

    [HttpPut("{bookId:guid}/availability")]
    public async Task<ActionResult<UpdateAvailabilityResponse>> UpdateAvailability(Guid bookId, [FromBody] UpdateAvailabilityRequest request)
    {
        var result = await _bookService.UpdateAvailabilityAsync(bookId, request.Delta);
        return Ok(result);
    }
}
