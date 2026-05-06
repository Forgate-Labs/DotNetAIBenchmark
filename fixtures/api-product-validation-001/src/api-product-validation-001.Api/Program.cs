var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var products = new List<Product>();

app.MapPost("/products", (CreateProductRequest request) =>
{
    var product = new Product(products.Count + 1, request.Name, request.Price);
    products.Add(product);
    return Results.Created($"/products/{product.Id}", product);
});

app.Run();

public sealed record CreateProductRequest(string Name, decimal Price);
public sealed record Product(int Id, string Name, decimal Price);
public partial class Program;
