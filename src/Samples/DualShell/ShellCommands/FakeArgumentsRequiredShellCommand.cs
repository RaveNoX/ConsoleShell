#region Usings

using System;
using System.Linq;
using ConsoleShell;

#endregion

namespace DualShell.ShellCommands
{
    internal class FakeArgumentsRequiredShellCommand : ShellCommandBase
    {
        public FakeArgumentsRequiredShellCommand(string pattern, string description) : base(pattern, description)
        {
        }

        public FakeArgumentsRequiredShellCommand(string pattern) : base(pattern)
        {
        }

        public override void Invoke(Shell shell, string[] args)
        {
            if (args.Any())
            {
                Console.WriteLine("Ivoke {0} \"{1}\" arguments: [ {2} ]", this, Pattern,
                    string.Join(", ", args));
            }
            else
            {
                throw new ShellCommandNotFoundException();
            }
        }
    }
}