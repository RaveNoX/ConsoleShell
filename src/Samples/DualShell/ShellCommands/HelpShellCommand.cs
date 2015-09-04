#region Usings

using System.Linq;
using ColoredConsole;
using ConsoleShell;

#endregion

namespace DualShell.ShellCommands
{
    internal class HelpShellCommand : ShellCommandBase
    {
        public HelpShellCommand() : base("help", "Prints this help")
        {
        }

        public override void Invoke(Shell shell, string[] args)
        {
            var items = shell.GetCommandsDescriptions(string.Join(" ", args));

            var padSize = items.Max(x => x.Key.Length) + 4;

            ColorConsole.WriteLine("Commands:".Cyan());
            foreach (var item in items)
            {
                ColorConsole.WriteLine("- ", item.Key.PadRight(padSize).White(), item.Value);
            }
        }
    }
}