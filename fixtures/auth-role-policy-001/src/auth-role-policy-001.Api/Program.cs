var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/admin/reports", (HttpRequest request) =>
{
    var role = request.Headers["X-Role"].ToString();
    return role == "Administrator" ? Results.Ok(new { status = "ready" }) : Results.StatusCode(StatusCodes.Status403Forbidden);
});

app.Run();

public partial class Program;
