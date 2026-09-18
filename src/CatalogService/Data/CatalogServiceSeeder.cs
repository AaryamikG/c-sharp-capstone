using CatalogService.Models;

namespace CatalogService.Data;

public static class CatalogServiceSeeder
{
    public static async Task SeedAsync(CatalogServiceDbContext context)
    {
        if (context.Books.Any())
        {
            return;
        }

        context.Books.AddRange(
            new Book
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-13-468599-1",
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Genre = "Technology",
                PublicationYear = 2008,
                Description = "A handbook of agile software craftsmanship",
                Publisher = "Prentice Hall",
                PageCount = 464,
                Language = "English",
                TotalCopies = 5,
                AvailableCopies = 2
            },
            new Book
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-13-475759-9",
                Title = "Refactoring",
                Author = "Martin Fowler",
                Genre = "Technology",
                PublicationYear = 2018,
                Description = "Improving the design of existing code",
                Publisher = "Addison-Wesley",
                PageCount = 448,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 0
            },
            new Book
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-596-00712-6",
                Title = "Head First Design Patterns",
                Author = "Eric Freeman",
                Genre = "Technology",
                PublicationYear = 2004,
                Description = "A brain-friendly guide to design patterns",
                Publisher = "O'Reilly Media",
                PageCount = 694,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 4
            },
            new Book
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-452-28423-4",
                Title = "1984",
                Author = "George Orwell",
                Genre = "Fiction",
                PublicationYear = 1949,
                Description = "A dystopian social science fiction novel",
                Publisher = "Secker & Warburg",
                PageCount = 328,
                Language = "English",
                TotalCopies = 6,
                AvailableCopies = 3
            },
            new Book
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-14-118776-1",
                Title = "One Hundred Years of Solitude",
                Author = "Gabriel Garcia Marquez",
                Genre = "Fiction",
                PublicationYear = 1967,
                Description = "The multi-generational story of the Buendia family",
                Publisher = "Harper & Row",
                PageCount = 417,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 1
            },
            new Book
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-7432-7356-5",
                Title = "The Da Vinci Code",
                Author = "Dan Brown",
                Genre = "Mystery",
                PublicationYear = 2003,
                Description = "A murder in the Louvre reveals a religious mystery",
                Publisher = "Doubleday",
                PageCount = 689,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 3
            });

        await context.SaveChangesAsync();
    }
}
