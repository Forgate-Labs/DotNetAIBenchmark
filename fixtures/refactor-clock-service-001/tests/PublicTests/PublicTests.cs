using FluentAssertions;
using RefactorClockService001.Core;

public sealed class PublicTests
{
    [Fact]
    public void IsExpired_should_use_injected_clock()
    {
        var clock = new FixedClock(new DateTimeOffset(2000, 1, 2, 0, 0, 0, TimeSpan.Zero));
        var service = new TrialService(clock);
        var trial = new Trial(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), 7);
        service.IsExpired(trial).Should().BeFalse();
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
