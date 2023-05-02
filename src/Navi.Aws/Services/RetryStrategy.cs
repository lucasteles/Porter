namespace Navi.Services;

public interface IRetryStrategy
{
    TimeSpan Evaluate(uint retryNumber);
}

sealed class Power2RetryStrategy : IRetryStrategy
{
    public TimeSpan Evaluate(uint retryNumber) => TimeSpan.FromSeconds(Math.Pow(2, retryNumber));
}

class FuncRetryStrategy : IRetryStrategy
{
    readonly Func<uint, TimeSpan> strategy;
    public FuncRetryStrategy(Func<uint, TimeSpan> strategy) => this.strategy = strategy;
    public TimeSpan Evaluate(uint retryNumber) => strategy(retryNumber);
}
