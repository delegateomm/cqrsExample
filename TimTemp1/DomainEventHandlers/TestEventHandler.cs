using System;
using TimTemp1.Abstractions;
using TimTemp1.DomainEvents;

namespace TimTemp1.DomainEventHandlers
{
    public class TestEventHandler : BaseDomainEventHandler
    {
        public void Handle(StartCreatingContractEvent evt)
        {
            Console.WriteLine($"{evt.GetType().Name} HANDLE");
        }

        public void Handle(FinishCreatingContractEvent evt)
        {
            Console.WriteLine($"{evt.GetType().Name} HANDLE");
        }
    }
}