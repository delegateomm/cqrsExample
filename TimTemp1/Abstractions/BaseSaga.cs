namespace TimTemp1.Abstractions
{
    /// <summary>
    /// For dynamic route correct method by the type of command.
    /// In child class need implementing overloaded methods void Handle() with concrete command type
    /// </summary>
    public abstract class BaseSaga : ISaga
    {
        /// <summary>
        /// in child class need implementing overloaded methods void Handle() with concrete command type
        /// </summary>
        /// <param name="command"></param>
        public void Execute(ICommand command)
        {
            ((dynamic) this).Handle((dynamic) command);
        }
    }
}