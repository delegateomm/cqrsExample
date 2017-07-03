using System;

namespace TimTemp1.Abstractions
{
    public interface ICommand
    {
        Guid Id { get; }

        DateTime Timestamp { get; }
    }
}