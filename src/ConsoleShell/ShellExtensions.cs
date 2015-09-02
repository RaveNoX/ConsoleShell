#region Usings

using System;

#endregion

namespace ConsoleShell
{
    public static class ShellExtensions
    {
        public static Shell AddLambdaCommand(this Shell shell, string pattern, string description,
            InvokeShellCommandDelegate invokeHandler, CompleteShellCommandDelegate completeHandler)
        {
            if (shell == null) throw new ArgumentNullException(nameof(shell));
            if (invokeHandler == null) throw new ArgumentNullException(nameof(invokeHandler));

            var command = new ShellCommand(pattern, description);
            command.InvokeCommand +=
                (sender, args) => { invokeHandler(args.Shell, sender as IShellCommand, args.Arguments); };

            if (completeHandler != null)
            {
                command.CompleteCommand +=
                    (sender, args) =>
                    {
                        args.Result = completeHandler(args.Shell, sender as IShellCommand, args.Tokens);
                    };
            }

            return shell.AddCommand(command);
        }

        public static Shell AddLambdaCommand(this Shell shell, string pattern, string description,
            InvokeShellCommandDelegate invokeHandler)
        {
            return shell.AddLambdaCommand(pattern, description, invokeHandler, null);
        }

        public static Shell AddLambdaCommand(this Shell shell, string pattern,
            InvokeShellCommandDelegate invokeHandler, CompleteShellCommandDelegate completeHandler)
        {
            return shell.AddLambdaCommand(pattern, null, invokeHandler, completeHandler);
        }

        public static Shell AddLambdaCommand(this Shell shell, string pattern,
            InvokeShellCommandDelegate invokeHandler)
        {
            return shell.AddLambdaCommand(pattern, invokeHandler, null);
        }
    }
}