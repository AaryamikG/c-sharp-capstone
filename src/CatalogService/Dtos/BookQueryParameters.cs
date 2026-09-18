namespace CatalogService.Dtos;

public class BookQueryParameters
{
    public int Page { get; set; } = 0;
    public int Size { get; set; } = 20;
    public string SortBy { get; set; } = "title";
    public string SortOrder { get; set; } = "asc";
    public string? Query { get; set; }
    public string? Genre { get; set; }
    public string? Isbn { get; set; }
    public bool AvailableOnly { get; set; } = false;
}
