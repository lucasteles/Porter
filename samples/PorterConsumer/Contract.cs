using Porter;

namespace PorterConsumer;

public record MyMessage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public int Age { get; set; }
}

public record MyMessage2
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class MyConsumer : IConsumer<MyMessage>
{
    readonly ILogger<MyConsumer> logger;

    public MyConsumer(ILogger<MyConsumer> logger) => this.logger = logger;

    public Task Consume(MyMessage message, CancellationToken ctx)
    {
        logger.LogInformation("Received new message: {M}", message.ToString());

        return Task.CompletedTask;
    }
}
