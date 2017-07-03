namespace TimTemp1.Abstractions
{
    /// <summary>
    /// For dynamic routing method to execute command
    /// </summary>
    public abstract class BaseApplicationService : IApplicationService
    {
        public IBus Bus { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bus">Ref for bus for sending commands</param>
        protected BaseApplicationService(IBus bus)
        {
            //TODO add repository to arguments
            Bus = bus;
        }
    }
}