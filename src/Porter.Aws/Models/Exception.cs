using System.Runtime.Serialization;

namespace Porter.Models;

/// <inheritdoc />
[Serializable]
public class PorterException : Exception
{
    internal PorterException(string message) : base(message) { }

    /// <inheritdoc />
    protected PorterException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
