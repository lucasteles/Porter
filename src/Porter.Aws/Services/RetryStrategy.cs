namespace Porter.Services;

public interface IRetryStrategy
{
    TimeSpan Evaluate(int retryNumber);
}

sealed class Power2RetryStrategy : IRetryStrategy
{
    public TimeSpan Evaluate(int retryNumber) => TimeSpan.FromSeconds(Math.Pow(2, retryNumber));
}

class FuncRetryStrategy : IRetryStrategy
{
    readonly Func<int, TimeSpan> strategy;
    public FuncRetryStrategy(Func<int, TimeSpan> strategy) => this.strategy = strategy;
    public TimeSpan Evaluate(int retryNumber) => strategy(retryNumber);
}
