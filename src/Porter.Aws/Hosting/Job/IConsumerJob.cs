namespace Porter.Hosting.Job;

interface IConsumerJob
{
    Task Start(IReadOnlyCollection<IConsumerDescriber> describers, CancellationToken stoppingToken);
}
