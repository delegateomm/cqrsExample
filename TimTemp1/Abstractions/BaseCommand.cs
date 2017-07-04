using System;

namespace TimTemp1.Abstractions
{
    public abstract class BaseCommand : ICommand
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; }
        public string SagaName { get; set; }

        protected BaseCommand()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }
    }
}