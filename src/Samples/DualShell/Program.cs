#region Usings

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using ColoredConsole;
using ConsoleShell;
using DualShell.ShellCommands;
using NDesk.Options;
using Newtonsoft.Json;

#endregion

namespace DualShell
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var historyFile = "history.txt";
            var shell = new Shell();
            var interactive = !args.Any();

            RegisterCommands(shell, interactive);

            if (interactive)
            {
                shell.WritePrompt += ShellOnWritePrompt;
                shell.ShellCommandNotFound += ShellOnShellCommandNotFound;
                shell.PrintAlternatives += ShellOnPrintAlternatives;

                if (File.Exists(historyFile))
                {
                    shell.History.Load(historyFile);
                }

                try
                {
                    shell.RunShell();
                }
                catch (ApplicationExitException)
                {
                }

                shell.History.Save(historyFile);                
            }
            else
            {
                try
                {
                    shell.ExecuteCommand(args);
                }
                catch (ShellCommandNotFoundException)
                {
                    ColorConsole.WriteLine("Invalid arguments".Red());
                }
            }
        }

        #region ShellEventHandlers

        private static void ShellOnPrintAlternatives(object sender, PrintAlternativesEventArgs e)
        {
            ColorConsole.WriteLine("Possible commands: ".Cyan());
            foreach (var alternative in e.Alternatives)
            {
                ColorConsole.WriteLine("- ", alternative.White());
            }
        }

        private static void ShellOnShellCommandNotFound(object sender, CommandNotFoundEventArgs e)
        {
            ColorConsole.WriteLine($"Command not found: ".Red(), e.Input.White());
        }

        private static void ShellOnWritePrompt(object sender, EventArgs eventArgs)
        {
            ColorConsole.Write("[ ".Green(), DateTime.Now.ToLongTimeString(), " ]-> ".Green());
        }

        #endregion

        #region RegisterCommands

        private static void RegisterCommands(Shell shell, bool interactive)
        {
            if (interactive)
            {
                shell.AddCommand("exit", "Exit from program", Exit);
                shell.AddCommand("quit", "Exit from program", Exit);
            }

            shell.AddCommand(new HelpShellCommand());
            shell.AddCommand("options", "Test options", InvokeTestOptions);

            shell.AddCommand(new FakeShellCommand("sip list", "list sip peers"));
            shell.AddCommand(new FakeShellCommand("sip add", "add sip peer"));
            shell.AddCommand(new FakeShellCommand("sip delete", "delete sip peer"));
            shell.AddCommand(new FakeShellCommand("sip acl list"));
            shell.AddCommand(new FakeShellCommand("sip acl add"));
            shell.AddCommand(new FakeShellCommand("sip acl delete"));
            shell.AddCommand(new FakeShellCommand("sip acl stick"));
            shell.AddCommand(new FakeShellCommand("sip acl flush"));

            shell.AddCommand(new FakeShellCommand("ip show"));

            shell.AddCommand(new CompleteMultipleFakeShellCommand("list"));
            shell.AddCommand(new CompleteOneFakeShellCommand("show"));

            shell.AddCommand(new AdditionalInputShellCommand());
        }

        #endregion

        #region Exit

        private static void Exit(Shell shell, IShellCommand shellCommand, string[] strings)
        {
            throw new ApplicationExitException();
        }

        #endregion

        #region InvokeTestOptions

        private static void InvokeTestOptions(Shell shell, IShellCommand shellCommand, string[] args)
        {
            var prefixes = new[] { "--", "-", "/" };

            var optionsArgs = args
                .TakeWhile(x => prefixes.Any(p => x.StartsWith(p) && x.Length > p.Length))
                .ToList();

            var parser = new OptionSet();

            var options = (dynamic)new ExpandoObject();
            options.Verbosity = 0;

            parser
                .Add("c|config=", "Set config path", s => { if (!string.IsNullOrWhiteSpace(s)) options.ConfigPath = s; })
                .Add("v|verbose", "increase verbosity level", s => { if (s != null) options.Verbosity++; });


            var arguments = new List<string>(parser.Parse(optionsArgs));
            arguments.AddRange(args.Skip(optionsArgs.Count));

            options.Arguments = arguments;

            Console.WriteLine("Options:");
            Console.WriteLine(JsonConvert.SerializeObject(options, Formatting.Indented));
        }

        #endregion
    }
}