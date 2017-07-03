using System;

namespace TimTemp1.Abstractions
{
    public abstract class BaseAggregate : IAggregate
    {
        public Guid Id { get; set; }
    }
}