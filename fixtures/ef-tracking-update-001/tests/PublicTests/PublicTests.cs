using EfTrackingUpdate001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class PublicTests
{
    [Fact]
    public async Task Rename_should_persist_new_name()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<UsersContext>().UseSqlite(connection).Options;
        await using var db = new UsersContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new AppUser { Name = "Old" });
        await db.SaveChangesAsync();

        var result = await new UserService(db).RenameAsync(1, "New");

        result.Should().BeTrue();
        (await db.Users.SingleAsync()).Name.Should().Be("New");
    }
}
