using TimTemp1.Abstractions.Enums;

namespace TimTemp1.Abstractions
{
    public sealed class CommandCompletionEvent : BaseDomainEvent, ICommandCompletionEvent
    {
        public CommandCompletionStatus CompletionStatus { get; set; }
    }
}