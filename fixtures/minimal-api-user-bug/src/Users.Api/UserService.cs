namespace Users.Api;

public sealed record User(int Id, string Name, bool Active);

public sealed class UserService
{
    private readonly List<User> _users =
    [
        new(1, "Ada Lovelace", true),
        new(2, "Grace Hopper", true),
        new(3, "Alan Turing", false)
    ];

    public User? FindById(int id)
    {
        // BUG: the lookup is shifted and returns the next user.
        return _users.FirstOrDefault(user => user.Id == id + 1);
    }
}
