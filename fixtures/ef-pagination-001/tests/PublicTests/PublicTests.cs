using EfPagination001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class PublicTests
{
    [Fact]
    public async Task GetPage_should_apply_page_size()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BlogContext>().UseSqlite(connection).Options;
        await using var db = new BlogContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Posts.AddRange(Enumerable.Range(1, 5).Select(i => new Post { Title = $"Post {i}" }));
        await db.SaveChangesAsync();

        var page = await new PostRepository(db).GetPageAsync(1, 2);
        page.Should().HaveCount(2);
    }
}
