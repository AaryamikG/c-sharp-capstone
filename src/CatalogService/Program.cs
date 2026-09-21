using System.Text.Json;
using System.Text.Json.Serialization;
using CatalogService.Data;
using CatalogService.Middleware;
using CatalogService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Catalog Service API", Version = "v1" });
});

var connectionString = builder.Configuration.GetConnectionString("CatalogDb");
builder.Services.AddDbContext<CatalogServiceDbContext>(options =>
{
    if (builder.Environment.IsDevelopment() || string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseInMemoryDatabase("CatalogServiceDb");
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddScoped<IBookService, BookService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogServiceDbContext>();
    await CatalogServiceSeeder.SeedAsync(dbContext);
}
else if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogServiceDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandling();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", async (CatalogServiceDbContext db) =>
{
    if (db.Database.IsRelational())
    {
        return Results.Ok(new
        {
            service = "CatalogService",
            database = db.Database.GetDbConnection().Database,
            migrations = (await db.Database.GetAppliedMigrationsAsync()).Count()
        });
    }

    return Results.Ok(new { service = "CatalogService", database = "in-memory", migrations = 0 });
});

app.Run();

public partial class Program;
