using Amazon.KeyManagementService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Porter.Clients;
using Porter.Extensions;
using Porter.Models;
using Porter.Services;
using Testcontainers.LocalStack;

[assembly: LevelOfParallelism(5)]

namespace Porter.Aws.Tests.TestUtils.Fixtures;

[Parallelizable(ParallelScope.Self)]
public class LocalstackFixture : ServicesFixture
{
    protected PorterConfig config = null!;
    protected string kmsTestKeyId = "";
    LocalStackContainer localstack = null!;

    string ServiceUrl =>
        new UriBuilder(localstack.GetConnectionString()) { Scheme = Uri.UriSchemeHttp }.Uri.ToString();

    protected override async Task BeforeSetup()
    {
        localstack = new LocalStackBuilder()
            .WithImage("localstack/localstack:latest")
            .Build();

        await localstack.StartAsync();
    }

    protected override void ConfigurePorter(PorterConfig c)
    {
        c.ServiceUrl = ServiceUrl;

        c.PubKey = $"alias/{faker.Random.Replace("Key?##?")}";
        c.Prefix = faker.Random.Replace("?##");
        c.Source = faker.Person.LastName.OnlyLetterOrDigit().ToPascalCase();

        c.QueueMaxReceiveCount = 1;
        c.MessageRetentionInDays = 1;
        c.RetriesBeforeDeadLetter = 1;
        c.MessageTimeoutInSeconds = 20;

        c.MessageDelayInSeconds = 0;
        c.LongPollingWaitInSeconds = 1;
        c.MapConsumerEndpoints = false;
        c.RaiseExceptions = false;
    }

    [TearDown]
    public async Task LocalstackTearDown() => await localstack.DisposeAsync();

    [SetUp]
    public async Task LocalstackSetup()
    {
        config = GetService<IOptions<PorterConfig>>().Value;
        GetService<AwsSqs>().ClearCache();
        kmsTestKeyId = await CreateDefaultKmsKey();
    }

    async Task<string> CreateDefaultKmsKey()
    {
        var kms = GetService<IAmazonKeyManagementService>();
        var key = await kms.CreateKeyAsync(new() { Description = "Test key" });
        await kms.CreateAliasAsync(new()
        {
            AliasName = config.PubKey,
            TargetKeyId = key.KeyMetadata.KeyId
        });
        return key.KeyMetadata.KeyId;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<IRetryStrategy, NoRetryStrategy>();
    }

    public async Task WaitFor(Func<Task<bool>> checkTask, TimeSpan timeout, TimeSpan next)
    {
        async Task WaitLoop()
        {
            while (!await checkTask())
                await Task.Delay(next);
        }

        await WaitLoop().WaitAsync(timeout);
    }

    public Task WaitFor(Func<Task<bool>> checkTask, TimeSpan? timeout = null, TimeSpan? next = null) =>
        WaitFor(
            checkTask,
            timeout ?? TimeSpan.FromSeconds(5000),
            next ?? TimeSpan.FromMilliseconds(500)
        );
}

class NoRetryStrategy : IRetryStrategy
{
    public TimeSpan Evaluate(int retryNumber) => TimeSpan.Zero;
}
