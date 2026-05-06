using EfTrackingUpdate001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class HiddenTests
{
    [Fact]
    public async Task Rename_should_return_false_for_missing_user()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<UsersContext>().UseSqlite(connection).Options;
        await using var db = new UsersContext(options);
        await db.Database.EnsureCreatedAsync();
        var result = await new UserService(db).RenameAsync(999, "Nobody");
        result.Should().BeFalse();
    }
}
