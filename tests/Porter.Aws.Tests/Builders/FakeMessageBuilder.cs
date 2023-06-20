using Bogus;

namespace Porter.Aws.Tests.Builders;

class FakeMessageBuilder
{
    readonly DateTime datetime;
    readonly Guid id;
    readonly Guid correlationId;
    string body;
    int retryNumber;

    public FakeMessageBuilder()
    {
        var faker = new Faker();
        datetime = faker.Date.Soon().ToUniversalTime();
        id = faker.Random.Guid();
        correlationId = faker.Random.Guid();
        body = TestMessage.New().ToSnakeCaseJson();
    }

    public FakeMessageBuilder WithBody(string body)
    {
        this.body = body;
        return this;
    }

    public IMessage Generate()
    {
        var value = A.Fake<IMessage>();
        A.CallTo(() => value.MessageId).Returns(id);
        A.CallTo(() => value.CorrelationId).Returns(correlationId);
        A.CallTo(() => value.Datetime).Returns(datetime);
        A.CallTo(() => value.RetryNumber).Returns(retryNumber);
        A.CallTo(() => value.Body).Returns(body);
        return value;
    }

    public FakeMessageBuilder WithRetry(int retry)
    {
        retryNumber = retry;
        return this;
    }
}
