var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var stock = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["book"] = 3, ["pen"] = 10 };

app.MapPost("/inventory/{sku}/reservations", (string sku, ReservationRequest request) =>
{
    stock.TryGetValue(sku, out var available);
    stock[sku] = available - request.Quantity;
    return Results.Ok(new ReservationResponse(sku, stock[sku]));
});

app.Run();

public sealed record ReservationRequest(int Quantity);
public sealed record ReservationResponse(string Sku, int Remaining);
public partial class Program;
