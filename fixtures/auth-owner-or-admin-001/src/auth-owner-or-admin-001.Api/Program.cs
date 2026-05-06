var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var notes = new List<Note> { new(1, "ada", "Draft"), new(2, "grace", "Todo") };

app.MapDelete("/notes/{id:int}", (int id, HttpRequest request) =>
{
    var role = request.Headers["X-Role"].ToString();
    var note = notes.SingleOrDefault(item => item.Id == id);
    if (note is null) return Results.NotFound();
    if (role != "Admin") return Results.StatusCode(StatusCodes.Status403Forbidden);
    notes.Remove(note);
    return Results.NoContent();
});

app.Run();

public sealed record Note(int Id, string Owner, string Text);
public partial class Program;
