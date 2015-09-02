#region Usings

using System;

#endregion

namespace ConsoleShell
{
    public static class ShellExtensions
    {
        public static Shell AddLambdaCommand(this Shell shell, string pattern, string description,
            Action<ShellCommand, string[]> invoke, Func<ShellCommand, string[], string[]> complete)
        {
            if (shell == null) throw new ArgumentNullException(nameof(shell));
            if (invoke == null) throw new ArgumentNullException(nameof(invoke));

            var command = new ShellCommand(pattern, description);
            command.InvokeCommand += (sender, args) => { invoke(sender as ShellCommand, args.Arguments); };

            if (complete != null)
            {
                command.CompleteCommand +=
                    (sender, args) => { args.Result = complete(sender as ShellCommand, args.Tokens); };
            }

            return shell.AddCommand(command);
        }

        public static Shell AddLambdaCommand(this Shell shell, string pattern, string description,
            Action<ShellCommand, string[]> invoke)
        {
            return shell.AddLambdaCommand(pattern, description, invoke, null);
        }

        public static Shell AddLambdaCommand(this Shell shell, string pattern,
            Action<ShellCommand, string[]> invoke, Func<ShellCommand, string[], string[]> complete)
        {
            return shell.AddLambdaCommand(pattern, null, invoke, complete);
        }

        public static Shell AddLambdaCommand(this Shell shell, string pattern,
            Action<ShellCommand, string[]> invoke)
        {
            return shell.AddLambdaCommand(pattern, invoke, null);
        }
    }
}