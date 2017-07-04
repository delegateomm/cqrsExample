namespace TimTemp1.Abstractions
{
    public class BaseDomainEventHandler : IDomainEventHandler
    {
        public void HandleEvent(IDomainEvent domainEvent)
        {
            ((dynamic) this).Handle((dynamic) domainEvent);
        }
    }
}