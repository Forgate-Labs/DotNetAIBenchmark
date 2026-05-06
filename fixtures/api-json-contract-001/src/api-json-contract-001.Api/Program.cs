var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var profiles = new[] { new Profile(1, "Ada Lovelace", true), new Profile(2, "Grace Hopper", false) };

app.MapGet("/profiles/{id:int}", (int id) =>
{
    var profile = profiles.FirstOrDefault(item => item.Id == id);
    return profile is null ? Results.NotFound() : Results.Ok(new { profile.Id, profile.Name, profile.Active });
});

app.Run();

public sealed record Profile(int Id, string Name, bool Active);
public partial class Program;
