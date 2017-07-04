using System;

namespace TimTemp1.Abstractions
{
    public abstract class BaseDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }

        protected BaseDomainEvent()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }
    }
}