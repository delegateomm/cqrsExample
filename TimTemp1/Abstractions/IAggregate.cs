using System;

namespace TimTemp1.Abstractions
{
    /// <summary>
    /// Base aggregate interface
    /// </summary>
    public interface IAggregate
    {
        Guid Id { get; set; }
    }
}