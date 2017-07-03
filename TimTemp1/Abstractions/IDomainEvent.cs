using System;

namespace TimTemp1.Abstractions
{
    public interface IDomainEvent
    {
        Guid Id { get; }
        DateTime Timestamp { get; }
    }
}