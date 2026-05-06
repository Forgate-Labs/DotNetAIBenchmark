namespace RefactorClockService001.Core;

public sealed record Trial(DateTimeOffset CreatedAtUtc, int LengthInDays);

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class TrialService(IClock clock)
{
    public bool IsExpired(Trial trial)
    {
        return DateTimeOffset.UtcNow > trial.CreatedAtUtc.AddDays(trial.LengthInDays);
    }
}
