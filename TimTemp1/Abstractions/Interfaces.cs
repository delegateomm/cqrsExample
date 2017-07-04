using System;
using System.Threading.Tasks;
using TimTemp1.Abstractions.Enums;

namespace TimTemp1.Abstractions
{
    /// <summary>
    /// Base aggregate interface
    /// </summary>
    public interface IAggregate
    {
        Guid Id { get; set; }
    }

    /// <summary>
    /// Interfaces for aggregate sevices
    /// </summary>
    public interface IApplicationService
    {
        /// <summary>
        /// Bus for sending command and events through the queue
        /// </summary>
        IBus Bus { get; }
    }

    public interface IBus
    {
        void SendCommand<T>(T command) where T : ICommand;

        void RaiseEvent<T>(T domainEvent) where T : IDomainEvent;

        Task<CommandCompletionStatus> WaitCommandCompletion(Guid commandId, TimeSpan timeout);

        void RegisterSaga<T>() where T : ISaga;

        void RegisterHandler<T>() where T : IDomainEventHandler;
    }

    public interface IDomainEventHandler
    {
        void HandleEvent(IDomainEvent domainEvent);
    }

    public interface ICommand
    {
        Guid Id { get; set; }

        DateTime Timestamp { get; }

        string SagaName { get; }
    }

    public interface ICommandCompletionEvent : IDomainEvent
    {
        CommandCompletionStatus CompletionStatus { get; }
    }

    public interface IDomainEvent
    {
        Guid Id { get; }
        DateTime Timestamp { get; }
    }

    public interface ISaga
    {
        /// <summary>
        /// Root method for execute command depebding on Type
        /// </summary>
        /// <param name="command">command to execute</param>
        void Execute(ICommand command);
    }
}