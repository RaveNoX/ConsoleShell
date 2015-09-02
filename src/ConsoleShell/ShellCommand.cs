#region Usings

using System;

#endregion

namespace ConsoleShell
{
    /// <summary>
    /// Command item for <see cref="Shell"/>
    /// </summary>
    public class ShellCommand : ShellCommandBase
    {
        /// <summary>
        /// Creates <see cref="ShellCommand"/> instance
        /// </summary>
        /// <param name="pattern">command text pattern</param>
        /// <param name="description">command description</param>
        public ShellCommand(string pattern, string description) : base(pattern, description)
        {
        }

        /// <summary>
        /// Creates <see cref="ShellCommand"/> instance
        /// </summary>
        /// <param name="pattern">command text pattern</param>        
        public ShellCommand(string pattern)
            : base(pattern)
        {
        }

        /// <summary>
        /// Executes when command needs to be invoked
        /// </summary>
        public event EventHandler<InvokeShellCommandEventArgs> InvokeCommand;

        /// <summary>
        /// Executes for tab-completion of command arguments
        /// </summary>
        public event EventHandler<CompleteShellCommandEventArgs> CompleteCommand;

        public override void Invoke(Shell shell, string[] args)
        {
            InvokeCommand?.Invoke(this, new InvokeShellCommandEventArgs(shell, args));
        }

        public override string[] Complete(Shell shell, string[] tokens)
        {
            var args = new CompleteShellCommandEventArgs(shell, tokens);
            CompleteCommand?.Invoke(this, args);
            return args.Result;
        }
    }
}