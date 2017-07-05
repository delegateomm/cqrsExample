using System;
using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<Guid, TaskCompletionSource<CommandCompletionEvent>> _commandsTasks;
        private readonly IConnection _rabbitConnection;
        private readonly IModel _rabbitChannel;
        private readonly string _personalEventsQueueName;
        private HashSet<Type> _registeredSagas;
        private HashSet<Type> _registeredEventHandlers;
        private List<Type> _knownDomainEventTypes;
        private List<Type> _knownCommandTypes;

        private readonly EventingBasicConsumer _eventConsumer;
        private EventingBasicConsumer _commandConsumer;

        private const string CommandBusQueueName = "commandQueue";
        private const string CommandsExchangerName = "commandsExchanger";
        private const string EventsExchangerName = "eventsExchanger";
        private const string CommandEventsRoutingPrefix = "commandResult.";
        private const string EventsRoutingPrefix = "event.";
        private const string CommandsRoutingPrefix = "command.";
        private string _rmqUser = "tempCqrsBusUser";
        private string _rmqPassword = "qazWSX123";

        public RabbitMqBus(string serverUri)
        {
            _commandsTasks = new Dictionary<Guid, TaskCompletionSource<CommandCompletionEvent>>();
            _registeredSagas = new HashSet<Type>();
            _registeredEventHandlers = new HashSet<Type>();
            _knownDomainEventTypes = new List<Type>();
            _knownCommandTypes = new List<Type>();

            var connectionFactory = new ConnectionFactory
            {
                HostName = serverUri,
                UserName = _rmqUser,
                Password = _rmqPassword,
                VirtualHost = "dev"
            };
            _rabbitConnection = connectionFactory.CreateConnection();
            _rabbitChannel = _rabbitConnection.CreateModel();

            /* TODO сделать опциональной независимую очередь со всеми командами
            _rabbitChannel.QueueDeclare(CommandBusQueueName, true, false, false);
            _rabbitChannel.ExchangeDeclare(CommandsExchangerName, "topic");
            _rabbitChannel.QueueBind(CommandBusQueueName, CommandsExchangerName, "#");
            */

            var properties = _rabbitChannel.CreateBasicProperties();
            properties.Persistent = true;

            _rabbitChannel.ExchangeDeclare(EventsExchangerName, "topic");

            _personalEventsQueueName = _rabbitChannel.QueueDeclare().QueueName;

            _eventConsumer = new EventingBasicConsumer(_rabbitChannel);
            _eventConsumer.Received += OnEventRecieved;
            _rabbitChannel.BasicConsume(_personalEventsQueueName, true, _eventConsumer);

            _commandConsumer = new EventingBasicConsumer(_rabbitChannel);
            _commandConsumer.Received += OnCommandRecieved;
        }

        public void Dispose()
        {
            _eventConsumer.Received -= OnEventRecieved;
            _rabbitConnection?.Dispose();
            _rabbitChannel?.Dispose();
        }

        public void RegisterSaga<T>() where T : ISaga
        {
            var sagaType = typeof(T);

            var handledCommandTypes = sagaType.GetMethods()
                .Where(w =>
                {
                    var args = w.GetParameters();
                    if (args.Length != 1)
                        return false;
                    var argType = args[0].ParameterType;
                    return
                        w.ReturnType == typeof(void) && w.Name == "Handle"
                        && typeof(ICommand).IsAssignableFrom(argType) && argType.IsClass;
                })
                .Select(s => s.GetParameters()[0].ParameterType);

            foreach (var commantType in handledCommandTypes)
            {
                var sagaCommandQueueName = CommandBusQueueName + "." + sagaType.Name + "." + commantType.Name;

                var routingKey = CommandsRoutingPrefix + sagaType.Name + "." + commantType.Name;

                _rabbitChannel.QueueDeclare(sagaCommandQueueName, true, false, false);
                _rabbitChannel.QueueBind(sagaCommandQueueName, CommandsExchangerName, routingKey);
                _rabbitChannel.BasicConsume(sagaCommandQueueName, false, _commandConsumer);
            }

            _registeredSagas.Add(sagaType);
            

        }

        public void RegisterHandler<T>() where T : IDomainEventHandler
        {
            var handlerType = typeof(T);

            var handledEventTypes = handlerType.GetMethods()
               .Where(w =>
               {
                   var args = w.GetParameters();
                   if (args.Length != 1)
                       return false;
                   var argType = args[0].ParameterType;
                   return
                       w.ReturnType == typeof(void) && w.Name == "Handle"
                       && typeof(IDomainEvent).IsAssignableFrom(argType) && argType.IsClass;
               })
               .Select(s => s.GetParameters()[0].ParameterType);

            foreach (var eventType in handledEventTypes)
            {
                var routingKey = EventsRoutingPrefix + eventType.Name;
                
                _rabbitChannel.QueueBind(_personalEventsQueueName, EventsExchangerName, routingKey);
            }

            _registeredEventHandlers.Add(handlerType);
        }

        public void SendCommand<T>(T command) where T : ICommand
        {
            var tcs = new TaskCompletionSource<CommandCompletionEvent>();
            _commandsTasks.Add(command.Id, tcs);

            var bodyMessage = command.ToRabbitMessageByteArray();

            _rabbitChannel.QueueBind(_personalEventsQueueName, EventsExchangerName,
                CommandEventsRoutingPrefix + command.Id);

            var routingKey = CommandsRoutingPrefix + command.SagaName + "." + typeof(T).Name;

            _rabbitChannel.BasicPublish(CommandsExchangerName, routingKey, null, bodyMessage);
        }

        public void RaiseEvent<T>(T domainEvent) where T : IDomainEvent
        {
            var bodyMessage = domainEvent.ToRabbitMessageByteArray();

            var routingKey = EventsRoutingPrefix + typeof(T).Name;

            _rabbitChannel.BasicPublish(EventsExchangerName, routingKey, null, bodyMessage);
        }

        public void RaiseCommandCompletionEventEvent(ICommandCompletionEvent commandCompletionEvent)
        {
            var bodyMessage = commandCompletionEvent.ToRabbitMessageByteArray();

            var routingKey = CommandEventsRoutingPrefix + commandCompletionEvent.Id;

            _rabbitChannel.BasicPublish(EventsExchangerName, routingKey, null, bodyMessage);
        }

        public Task<CommandCompletionEvent> WaitCommandCompletion(Guid commandId, TimeSpan timeout)
        {
            if (!_commandsTasks.ContainsKey(commandId))
                throw new Exception("Command not found"); //TODO завести свой класс на исключение

            var commandTask = _commandsTasks[commandId].Task;

            return commandTask;
        }

        public void RigesterDomainEventsTypes(IEnumerable<Type> types)
        {
            _knownDomainEventTypes.AddRange(types);
        }

        public void RigesterCommandTypes(IEnumerable<Type> types)
        {
            _knownCommandTypes.AddRange(types);
        }

        //TODO - много вопросов как сделать красиво выполнение комманд сагами,
        //TODO - если возникнет ситуация когда не будет обработчика ни в одной подписанной саге у шины (сообщение из очереди будет уже поглощено)
        private void OnCommandRecieved(object sender, BasicDeliverEventArgs args)
        {
            var routingSplit = args.RoutingKey.Split('.');

            // Шаблон - "command.@SagaName.@CommandName"
            var commandTypeStringName = routingSplit[2];
            var sagaTypeName = routingSplit[1];

            //TODO пока не знаю как лучше сделать - в будущем убрать костыльную привязку к пространству имен
            var commandInstance =
                (ICommand) GetInstanceFromMessagesBytesByType(args.Body,
                    _knownCommandTypes.FirstOrDefault(f => f.Name == commandTypeStringName));

            var sagaType = _registeredSagas.FirstOrDefault(f => f.Name == sagaTypeName);
            // Создаю экземпляр Саги с конструктором вида Saga(IBus bus)
            var instanceOfSaga = (ISaga) Activator.CreateInstance(sagaType, this);
            instanceOfSaga.Execute(commandInstance);
            _rabbitChannel.BasicAck(args.DeliveryTag, false);
        }

        private object GetInstanceFromMessagesBytesByType(byte[] messageBody, Type type)
        {
            MethodInfo byteArrayToObjectMethod = typeof(RabbitMqBus).GetMethod("ByteArrayToObject",
                BindingFlags.NonPublic | BindingFlags.Static);

            byteArrayToObjectMethod = byteArrayToObjectMethod.MakeGenericMethod(type);
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
                {
                    _commandsTasks[commandId].TrySetResult(commandCompletionEvent);
                    _commandsTasks.Remove(commandId);
                }

                _rabbitChannel.QueueUnbind(_personalEventsQueueName, EventsExchangerName, args.RoutingKey);
                return;
            }

            //TODO finish
            var eventClassName = args.RoutingKey.Replace(EventsRoutingPrefix, string.Empty);
            var eventInstance =
                (IDomainEvent) GetInstanceFromMessagesBytesByType(args.Body,
                    _knownDomainEventTypes.FirstOrDefault(f => f.Name == eventClassName));

            foreach (var eventHandlerType in _registeredEventHandlers)
            {
                // Создаю экземпляр Саги с конструктором вида EventHandler()
                var instanceOfEventHandler = (IDomainEventHandler) Activator.CreateInstance(eventHandlerType);
                instanceOfEventHandler.HandleEvent(eventInstance);
            }

        }
    }
}