using Microsoft.EntityFrameworkCore;

namespace EfPagination001.Core;

public sealed class BlogContext(DbContextOptions<BlogContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
}

public sealed class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public sealed class PostRepository(BlogContext db)
{
    public Task<List<Post>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return db.Posts.OrderBy(post => post.Id).ToListAsync(cancellationToken);
    }
}
