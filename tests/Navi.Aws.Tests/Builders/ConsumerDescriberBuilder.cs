using Navi;
using Navi.Hosting;

namespace Navi.Aws.Tests.Builders;

class ConsumerDescriberBuilder
{
    Type consumerType = typeof(FakeMessageConsumer<TestMessage>);
    Func<Exception, Task>? errorHandler;
    int maxConcurrency = 1;
    Type messageType = typeof(TestMessage);
    TimeSpan pollingInterval = TimeSpan.FromMilliseconds(1);
    string topicName = "good_name";

    public ConsumerDescriberBuilder UsingConsumer<TConsumer, TMessage>() where TConsumer : IMessageConsumer<TMessage>
        where TMessage : notnull =>
        WithConsumerType<TConsumer>()
            .WithMessageType<TMessage>();

    public ConsumerDescriberBuilder WithValidConsumerType<T>() where T : IWeakConsumer =>
        WithConsumerType<T>();

    public ConsumerDescriberBuilder WithConsumerType<T>() =>
        WithConsumerType(typeof(T));

    public ConsumerDescriberBuilder WithConsumerType(Type type)
    {
        consumerType = type;
        return this;
    }

    public ConsumerDescriberBuilder WithMessageType<T>() =>
        WithMessageType(typeof(T));

    public ConsumerDescriberBuilder WithMessageType(Type type)
    {
        messageType = type;
        return this;
    }

    public ConsumerDescriberBuilder WithTopicName(string name)
    {
        topicName = name;
        return this;
    }

    public ConsumerDescriberBuilder WithConcurrency(int max)
    {
        maxConcurrency = max;
        return this;
    }

    public ConsumerDescriberBuilder WithPolling(TimeSpan interval)
    {
        pollingInterval = interval;
        return this;
    }

    public ConsumerDescriberBuilder WithErrorHandler() =>
        WithErrorHandler(A.Fake<Func<Exception, Task>>());

    public ConsumerDescriberBuilder WithErrorHandler(Func<Exception, Task> handler)
    {
        errorHandler = handler;
        return this;
    }

    public IConsumerDescriber Generate()
    {
        var value = A.Fake<IConsumerDescriber>();
        A.CallTo(() => value.TopicName).Returns(topicName);
        A.CallTo(() => value.ConsumerType).Returns(consumerType);
        A.CallTo(() => value.MessageType).Returns(messageType);
        A.CallTo(() => value.MaxConcurrency).Returns(maxConcurrency);
        A.CallTo(() => value.PollingInterval).Returns(pollingInterval);
        A.CallTo(() => value.ErrorHandler).Returns(errorHandler);
        return value;
    }
}
