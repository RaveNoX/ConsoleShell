#region Usings

using System;

#endregion

namespace ConsoleShell
{
    /// <summary>
    /// Command item for <see cref="Shell"/>
    /// </summary>
    public class ShellCommand
    {
        /// <summary>
        /// Creates <see cref="ShellCommand"/> instance
        /// </summary>
        /// <param name="pattern">command text pattern</param>
        /// <param name="description">command description</param>
        public ShellCommand(string pattern, string description)
        {
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentNullException(nameof(pattern));

            Pattern = pattern.Trim();
            Description = description;
        }

        /// <summary>
        /// Creates <see cref="ShellCommand"/> instance
        /// </summary>
        /// <param name="pattern">command text pattern</param>        
        public ShellCommand(string pattern)
            : this(pattern, null)
        {
        }

        /// <summary>
        /// Command text pattern
        /// </summary>
        public string Pattern { get; private set; }

        /// <summary>
        /// Command description
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Executes when command needs to be invoked
        /// </summary>
        public event EventHandler<InvokeShellCommandEventArgs> InvokeCommand;

        /// <summary>
        /// Executes for tab-completion of command arguments
        /// </summary>
        public event EventHandler<CompleteShellCommandEventArgs> CompleteCommand;

        internal void DoInvoke(Shell shell, string[] args)
        {
            InvokeCommand?.Invoke(this, new InvokeShellCommandEventArgs(shell, args));
        }

        internal string[] DoComplete(Shell shell, string[] tokens)
        {
            var args = new CompleteShellCommandEventArgs(shell, tokens);
            CompleteCommand?.Invoke(this, args);

            return args.Result;
        }        
    }
}