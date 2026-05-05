using Users.Api;

var service = new UserService();

AssertUser(1, "Ada Lovelace", active: true);
AssertUser(2, "Grace Hopper", active: true);
AssertUser(3, "Alan Turing", active: false);
AssertMissing(999);

Console.WriteLine("All fixture checks passed.");

void AssertUser(int id, string expectedName, bool active)
{
    var user = service.FindById(id);
    if (user is null)
    {
        throw new InvalidOperationException($"Expected user {id}, got null.");
    }

    if (user.Id != id || user.Name != expectedName || user.Active != active)
    {
        throw new InvalidOperationException(
            $"Expected ({id}, {expectedName}, {active}), got ({user.Id}, {user.Name}, {user.Active}).");
    }
}

void AssertMissing(int id)
{
    var user = service.FindById(id);
    if (user is not null)
    {
        throw new InvalidOperationException($"Expected missing user {id}, got {user}.");
    }
}
