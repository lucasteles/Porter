using Navi.Models;

namespace Navi.Hosting;

interface IProducerDescriber
{
    string TopicName { get; }
    Type MessageType { get; }
    TopicNameOverride? NameOverride { get; }
}

sealed record ProducerDescriber
    (string TopicName, Type MessageType, TopicNameOverride? NameOverride) : IProducerDescriber;
