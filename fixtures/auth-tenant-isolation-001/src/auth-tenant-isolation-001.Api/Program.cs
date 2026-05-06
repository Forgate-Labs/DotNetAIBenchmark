var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var documents = new[] { new Document(1, "tenant-a", "A secret"), new Document(2, "tenant-b", "B secret") };

app.MapGet("/documents/{id:int}", (int id) =>
{
    var document = documents.SingleOrDefault(item => item.Id == id);
    return document is null ? Results.NotFound() : Results.Ok(document);
});

app.Run();

public sealed record Document(int Id, string TenantId, string Title);
public partial class Program;
