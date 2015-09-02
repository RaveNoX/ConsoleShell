#region Usings

using System;

#endregion

namespace ConsoleShell
{
    /// <summary>
    /// Abstract implementation of <see cref="IShellCommand"/>
    /// </summary>
    public abstract class ShellCommandBase : IShellCommand
    {
        /// <summary>
        ///     Creates <see cref="ShellCommandBase" /> instance
        /// </summary>
        /// <param name="pattern">command text pattern</param>
        /// <param name="description">command description</param>
        protected ShellCommandBase(string pattern, string description)
        {
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentNullException(nameof(pattern));

            Pattern = pattern.Trim();
            Description = description;
        }

        /// <summary>
        ///     Creates <see cref="ShellCommandBase" /> instance
        /// </summary>
        /// <param name="pattern">command text pattern</param>
        protected ShellCommandBase(string pattern)
            : this(pattern, null)
        {
        }

        /// <summary>
        ///     Command text pattern
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        ///     Command description
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     Executes when need to invoke command
        /// </summary>
        /// <param name="shell">
        ///     <see cref="Shell" />
        /// </param>
        /// <param name="args">arguments array</param>
        public abstract void Invoke(Shell shell, string[] args);

        /// <summary>
        ///     Executes when need to complete input
        /// </summary>
        /// <param name="shell">
        ///     <see cref="Shell" />
        /// </param>
        /// <param name="tokens">tokens array</param>
        /// <returns></returns>
        public abstract string[] Complete(Shell shell, string[] tokens);
    }
}