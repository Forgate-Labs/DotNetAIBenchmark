using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/orders/quote", (OrderQuoteRequest request) =>
{
    if (request.Lines.Count == 0 || request.Lines.Any(line => line.Quantity <= 0 || line.UnitPrice < 0))
    {
        return Results.BadRequest(new { error = "Invalid order lines" });
    }

    var total = request.Lines.Sum(line => line.UnitPrice);
    return Results.Ok(new OrderQuoteResponse(total));
});

app.Run();

public sealed record OrderQuoteRequest(List<OrderLine> Lines);
public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);
public sealed record OrderQuoteResponse(decimal Total);
public partial class Program;
