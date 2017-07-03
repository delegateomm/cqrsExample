namespace TimTemp1.Abstractions
{
    public interface ISaga
    {
        /// <summary>
        /// Root method for execute command depebding on Type
        /// </summary>
        /// <param name="command">command to execute</param>
        void Execute(ICommand command);
    }
}