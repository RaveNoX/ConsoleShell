namespace ConsoleShell
{
    public interface IShellCommand
    {
        /// <summary>
        /// Command text pattern
        /// </summary>
        string Pattern { get; }

        /// <summary>
        /// Command description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes when need to invoke command
        /// </summary>
        /// <param name="shell"><see cref="Shell"/></param>
        /// <param name="args">arguments array</param>
        void Invoke(Shell shell, string[] args);

        /// <summary>
        /// Executes when need to complete input
        /// </summary>
        /// <param name="shell"><see cref="Shell"/></param>
        /// <param name="tokens">tokens array</param>
        /// <returns></returns>
        string[] Complete(Shell shell, string[] tokens);
    }
}