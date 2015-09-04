#region Usings

using System;
using ConsoleShell;

#endregion

namespace DualShell.ShellCommands
{
    internal class FakeShellCommand : ShellCommandBase
    {
        public FakeShellCommand(string pattern, string description) : base(pattern, description)
        {
        }

        public FakeShellCommand(string pattern) : base(pattern)
        {
        }

        public override void Invoke(Shell shell, string[] args)
        {
            Console.WriteLine("Ivoke {0} \"{1}\" arguments: [ {2} ]", this, Pattern, string.Join(", ", args));
        }
    }
}