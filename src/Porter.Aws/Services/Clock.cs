namespace Porter.Services;

public interface IPorterClock
{
    DateTime Now();
}

sealed class UtcClock : IPorterClock
{
    public DateTime Now() => DateTime.UtcNow;
}
