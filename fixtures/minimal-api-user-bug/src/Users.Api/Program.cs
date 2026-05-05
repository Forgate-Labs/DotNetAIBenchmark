using Users.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var users = new UserService();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/users/{id:int}", (int id) =>
{
    var user = users.FindById(id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.Run();

public partial class Program;
