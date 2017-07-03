using System.Threading.Tasks;

namespace TimTemp1.Abstractions
{
    public interface IBus
    {
        Task SendCommand<T>(T command) where T : ICommand;

        void RaiseEvent<T>(T domainEvent) where T : IDomainEvent;
    }
}