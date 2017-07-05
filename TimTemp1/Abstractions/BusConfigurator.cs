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
            RegisterAllDomainEventTypes();
            RegisterAllComandTypes();
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

        private void RegisterAllDomainEventTypes()
        {
            var type = typeof(IDomainEvent);
            _bus.RigesterDomainEventsTypes(
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(sm => sm.GetTypes())
                    .Where(w => type.IsAssignableFrom(w) && w.IsClass));
        }

        private void RegisterAllComandTypes()
        {
            var type = typeof(ICommand);
            _bus.RigesterCommandTypes(
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(sm => sm.GetTypes())
                    .Where(w => type.IsAssignableFrom(w) && w.IsClass));
        }
    }
}