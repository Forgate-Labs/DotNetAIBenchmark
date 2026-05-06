using EfPagination001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class HiddenTests
{
    [Fact]
    public async Task GetPage_should_skip_previous_pages_and_keep_order()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BlogContext>().UseSqlite(connection).Options;
        await using var db = new BlogContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Posts.AddRange(Enumerable.Range(1, 5).Select(i => new Post { Title = $"Post {i}" }));
        await db.SaveChangesAsync();

        var page = await new PostRepository(db).GetPageAsync(2, 2);
        page.Select(post => post.Title).Should().Equal("Post 3", "Post 4");
    }
}
