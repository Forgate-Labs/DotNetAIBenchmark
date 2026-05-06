using System.Data.Common;
using EfNPlusOne001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class HiddenTests
{
    [Fact]
    public async Task List_should_use_bounded_number_of_queries()
    {
        var counter = new CommandCounterInterceptor();
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SalesContext>().UseSqlite(connection).AddInterceptors(counter).Options;
        await using var db = new SalesContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Customers.AddRange(Enumerable.Range(1, 5).Select(i => new Customer { Name = $"C{i}" }));
        db.Orders.AddRange(Enumerable.Range(1, 5).Select(i => new Order { CustomerId = i }));
        await db.SaveChangesAsync();
        counter.Count = 0;

        await new CustomerSummaryRepository(db).ListAsync();

        counter.Count.Should().BeLessThanOrEqualTo(2);
    }

    private sealed class CommandCounterInterceptor : DbCommandInterceptor
    {
        public int Count { get; set; }
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Count++;
            return base.ReaderExecuting(command, eventData, result);
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
