namespace TimTemp1.Abstractions
{
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
}