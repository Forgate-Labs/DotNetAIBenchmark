using FluentAssertions;
using RefactorClockService001.Core;

public sealed class HiddenTests
{
    [Fact]
    public void IsExpired_should_expire_at_exact_boundary()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 5, 8, 0, 0, 0, TimeSpan.Zero));
        var service = new TrialService(clock);
        var trial = new Trial(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), 7);
        service.IsExpired(trial).Should().BeTrue();
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
