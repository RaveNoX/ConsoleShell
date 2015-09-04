#region Usings

using System.Linq;
using ConsoleShell;

#endregion

namespace DualShell.ShellCommands
{
    internal class CompleteMultipleFakeShellCommand : FakeShellCommand
    {
        public CompleteMultipleFakeShellCommand(string pattern, string description) : base(pattern, description)
        {
        }

        public CompleteMultipleFakeShellCommand(string pattern) : base(pattern)
        {
        }

        public override string[] Complete(Shell shell, string[] tokens)
        {
            var items = new[] {"users", "peers"};

            if (tokens.Length == 0)
            {
                return items;
            }

            return tokens.Length == 1 ? items.Where(x => x.StartsWith(tokens[0])).ToArray() : null;
        }
    }
}