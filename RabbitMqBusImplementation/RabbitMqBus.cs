using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TimTemp1.Abstractions;
using TimTemp1.Abstractions.Enums;

namespace RabbitMqBusImplementation
{
    public class RabbitMqBus : IBus, IDisposable
    {
        private Dictionary<Guid, TaskCompletionSource<CommandCompletionStatus>> _commandsTasks;
        private readonly ConnectionFactory _connectionFactory;
        private readonly IConnection _rabbitConnection;
        private readonly IModel _rabbitChannel;
        private readonly string _personalEventsQueueName;
        private HashSet<Type> _registeredSagas;
        private HashSet<Type> _registeredEventHandlers;

        private readonly EventingBasicConsumer _eventConsumer;
        private EventingBasicConsumer _commandConsumer;

        private const string CommandBusQueueName = "commandQueue";
        private const string CommandsExchangerName = "commandsExchanger";
        private const string EventsExchangerName = "eventsExchanger";
        private const string CommandEventsRoutingPrefix = "commandResult.";
        private const string EventsRoutingPrefix = "event.";
        private const string CommandsRoutingPrefix = "command.";

        public RabbitMqBus(Uri serverUri)
        {
            _commandsTasks = new Dictionary<Guid, TaskCompletionSource<CommandCompletionStatus>>();
            _registeredSagas = new HashSet<Type>();
            _registeredEventHandlers = new HashSet<Type>();

            _connectionFactory = new ConnectionFactory {HostName = serverUri.ToString()};
            _rabbitConnection = _connectionFactory.CreateConnection();
            _rabbitChannel = _rabbitConnection.CreateModel();

            _rabbitChannel.QueueDeclare(CommandBusQueueName, true, false, false);
            _rabbitChannel.ExchangeDeclare(CommandsExchangerName, "topic");
            _rabbitChannel.QueueBind(CommandBusQueueName, CommandsExchangerName, "#");

            var properties = _rabbitChannel.CreateBasicProperties();
            properties.Persistent = true;

            _rabbitChannel.ExchangeDeclare(EventsExchangerName, "topic");

            _personalEventsQueueName = _rabbitChannel.QueueDeclare().QueueName;

            _eventConsumer = new EventingBasicConsumer(_rabbitChannel);
            _eventConsumer.Received += OnEventRecieved;
            _rabbitChannel.BasicConsume(_personalEventsQueueName, true, _eventConsumer);
        }

        public void Dispose()
        {
            _eventConsumer.Received -= OnEventRecieved;
            _rabbitConnection?.Dispose();
            _rabbitChannel?.Dispose();
        }

        public void RegisterSaga<T>() where T : ISaga
        {
            _registeredSagas.Add(typeof(T));
            if (_commandConsumer == null)
            {
                _commandConsumer = new EventingBasicConsumer(_rabbitChannel);
                _commandConsumer.Received += OnCommandRecieved;
                _rabbitChannel.BasicConsume(CommandBusQueueName, false, _commandConsumer);
            }
        }

        public void RegisterHandler<T>() where T : IDomainEventHandler
        {
            _registeredEventHandlers.Add(typeof(T));
        }

        public void SendCommand<T>(T command) where T : ICommand
        {
            TaskCompletionSource<CommandCompletionStatus> tcs = new TaskCompletionSource<CommandCompletionStatus>();
            _commandsTasks.Add(command.Id, tcs);

            var bodyMessage = command.ToGzipRabbitMessageByteArray();

            _rabbitChannel.QueueBind(_personalEventsQueueName, EventsExchangerName,
                CommandEventsRoutingPrefix + "." + command.Id);

            var routingKey = CommandsRoutingPrefix + typeof(T).Name;

            _rabbitChannel.BasicPublish(CommandsExchangerName, routingKey, null, bodyMessage);
        }

        public void RaiseEvent<T>(T domainEvent) where T : IDomainEvent
        {
            var bodyMessage = domainEvent.ToGzipRabbitMessageByteArray();

            var routingKey = typeof(T).Name;

            _rabbitChannel.BasicPublish(EventsExchangerName, routingKey, null, bodyMessage);
        }

        public Task<CommandCompletionStatus> WaitCommandCompletion(Guid commandId, TimeSpan timeout)
        {
            if (!_commandsTasks.ContainsKey(commandId))
                throw new Exception("Command not found"); //TODO завести свой класс на исключение

            var commandTask = _commandsTasks[commandId].Task;

            _commandsTasks.Remove(commandId);

            return commandTask;
        }

        //TODO - много вопросов как сделать красиво выполнение комманд сагами,
        //TODO - если возникнет ситуация когда не будет обработчика ни в одной подписанной саге у шины (сообщение из очереди будет уже поглощено)
        private void OnCommandRecieved(object sender, BasicDeliverEventArgs args)
        {
            var commandTypeStringName = args.RoutingKey.Replace(CommandsRoutingPrefix, string.Empty);

            //TODO пока не знаю как лучше сделать - в будущем убрать костыльную привязку к пространству имен
            var commandInstance =
                (ICommand)
                GetInstanceFromMessagesBytesByTypeName(args.Body, "TimTemp1.Commands." + commandTypeStringName);

            foreach (var sagaType in _registeredSagas)
            {
                // Создаю экземпляр Саги с конструктором вида Saga(IBus bus)
                var instanceOfSaga = (ISaga) Activator.CreateInstance(sagaType, this);
                instanceOfSaga.Execute(commandInstance);
            }
        }

        private object GetInstanceFromMessagesBytesByTypeName(byte[] messageBody, string fullTypeName)
        {
            var commandType = Type.GetType(fullTypeName);
            MethodInfo byteArrayToObjectMethod = typeof(RabbitMqBus).GetMethod("ByteArrayToObject",
                BindingFlags.NonPublic | BindingFlags.Static);

            byteArrayToObjectMethod = byteArrayToObjectMethod.MakeGenericMethod(commandType);
            object[] arguments = { messageBody };

            var objectInstance = byteArrayToObjectMethod.Invoke(null, arguments);

            return objectInstance;
        }

        // ReSharper disable once UnusedMember.Local
        // НЕ УДАЛЯТЬ!!!
        // Через reflection получаю метод в runtim'e для generic парса json объекта. Метод GetInstanceFromMessagesBytesByTypeName()
        private static T ByteArrayToObject<T>(byte[] source)
        {
            return source.ToObject<T>();
        }

        private void OnEventRecieved(object sender, BasicDeliverEventArgs args)
        {
            if (args.RoutingKey.StartsWith(CommandEventsRoutingPrefix))
            {
                var commandId = Guid.Parse(args.RoutingKey.Replace(CommandEventsRoutingPrefix, string.Empty));

                var commandCompletionEvent = args.Body.ToObject<CommandCompletionEvent>();

                if (_commandsTasks.ContainsKey(commandId))
                    _commandsTasks[commandId].TrySetResult(commandCompletionEvent.CompletionStatus);

                _rabbitChannel.QueueUnbind(_personalEventsQueueName, EventsExchangerName, args.RoutingKey);
                return;
            }

            //TODO finish
            var eventClassName = args.RoutingKey.Replace(EventsRoutingPrefix, string.Empty);
            var eventInstance =
                (IDomainEvent) GetInstanceFromMessagesBytesByTypeName(args.Body, "TimTemp1.DomainEvents." + eventClassName);

            foreach (var eventHandlerType in _registeredEventHandlers)
            {
                // Создаю экземпляр Саги с конструктором вида EventHandler()
                var instanceOfEventHandler = (IDomainEventHandler) Activator.CreateInstance(eventHandlerType);
                instanceOfEventHandler.HandleEvent(eventInstance);
            }

        }
    }
}