using Microsoft.EntityFrameworkCore;

namespace EfTrackingUpdate001.Core;

public sealed class UsersContext(DbContextOptions<UsersContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
}

public sealed class AppUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UserService(UsersContext db)
{
    public async Task<bool> RenameAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return false;
        user.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
