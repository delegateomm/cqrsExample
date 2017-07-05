using System;
using System.Linq;

namespace TimTemp1.Abstractions
{
    public sealed class BusConfigurator
    {
        private readonly IBus _bus;

        public BusConfigurator(IBus bus)
        {
            _bus = bus;
        }

        public BusConfigurator RegisterSaga<T>() where T : ISaga
        {
            _bus.RegisterSaga<T>();
            return this;
        }

        public BusConfigurator RegisterHandler<T>() where T : IDomainEventHandler
        {
            _bus.RegisterHandler<T>();
            return this;
        }

        public BusConfigurator RegisterAllDomainEventTypes()
        {
            var type = typeof(IDomainEvent);
            _bus.RigesterDomainEventsTypes(
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(sm => sm.GetTypes())
                    .Where(w => type.IsAssignableFrom(w)));
            return this;
        }

        public BusConfigurator RegisterAllComandTypes()
        {
            var type = typeof(ICommand);
            _bus.RigesterCommandTypes(
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(sm => sm.GetTypes())
                    .Where(w => type.IsAssignableFrom(w)));
            return this;
        }
    }
}