namespace Navi.Services;

public interface INaviClock
{
    DateTime Now();
}

sealed class UtcClock : INaviClock
{
    public DateTime Now() => DateTime.UtcNow;
}
