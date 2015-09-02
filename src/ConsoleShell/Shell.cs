using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleShell.Internal;
using ConsoleShell.Readline;

namespace ConsoleShell
{
    public class Shell
    {
        #region Fields

        private ShellCommandsContainer _container = new ShellCommandsContainer();
        private readonly object _lock = new object();

        #endregion

        public event EventHandler<CommandNotFoundEventArgs> ShellCommandNotFound;
        public event EventHandler<PrintAlternativesEventArgs> PrintAlternatives;
        public event EventHandler ShellInterrupt;
        public event EventHandler WritePrompt;

        public bool CtrlCInterrupts { get; set; } = Path.DirectorySeparatorChar == '/';
        public bool CtrlDIsEOF { get; set; } = true;
        public bool CtrlZIsEOF { get; set; } = Path.DirectorySeparatorChar == '\\';

        public ShellHistory History { get; } = new ShellHistory();

        public void RunShell()
        {
            var readline = new Readline.Readline(History)
            {
                CtrlCInterrupts = CtrlCInterrupts,
                CtrlDIsEOF = CtrlDIsEOF,
                CtrlZIsEOF = CtrlZIsEOF
            };

            readline.WritePrompt += ReadlineOnWritePrompt;
            readline.Interrupt += (sender, args) => ShellInterrupt?.Invoke(this, EventArgs.Empty);
            readline.TabComplete += ReadlineOnTabComplete;
            readline.PrintAlternatives += (sender, args) => OnPrintAlternatives(args);

            while (true)
            {
                var input = readline.ReadLine();
                input = input.Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                try
                {
                    ExecuteCommand(input);
                }
                catch (ShellCommandNotFoundException ex)
                {
                    OnShellCommandNotFound(input, ex.Tokens.ToArray());
                }

                History.AddUnique(input);
            }
        }

        private void ReadlineOnWritePrompt(object sender, EventArgs eventArgs)
        {
            var handler = WritePrompt;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
            else
            {
                Console.Write(">");
            }
        }

        public string ReadPassword(Action writePromtAction)
        {
            var readline = new Readline.Readline(History)
            {
                CtrlCInterrupts = CtrlCInterrupts,
                CtrlDIsEOF = CtrlDIsEOF,
                CtrlZIsEOF = CtrlZIsEOF
            };

            readline.WritePrompt += (sender, args) => WritePrompt?.Invoke(this, EventArgs.Empty);
            readline.Interrupt += (sender, args) => ShellInterrupt?.Invoke(this, EventArgs.Empty);

            return readline.ReadPassword();
        }

        private void ReadlineOnTabComplete(object sender, TabCompleteEventArgs e)
        {
            var buff = ((Readline.Readline)sender).LineBuffer;

            lock (_lock)
            {
                var complete = _container.CompleteInput(this, buff).ToArray();

                if (complete.Length == 1)
                {
                    e.Output = complete.First() + " ";
                }
                else if (complete.Length > 1)
                {
                    e.Alternatives = complete;
                }
            }
        }

        #region Execute

        public void ExecuteCommand(string[] tokens)
        {
            Action command;
            lock (_lock)
            {
                command = _container.FindCommand(this, tokens);
            }

            if (command == null)
            {
                throw new ShellCommandNotFoundException(tokens);
            }

            command();
        }

        public void ExecuteCommand(string input)
        {
            ExecuteCommand(ShellCommandTokenizer.Tokenize(input).ToArray());
        }

        #endregion

        #region Commands manipulation

        public SortedList<string, string> GetCommandsDescriptions(string prefix = null)
        {
            lock (_lock)
            {
                return _container.GetDescriptions(prefix);
            }
        }

        public Shell AddCommand(IShellCommand command)
        {
            lock (_lock)
            {
                _container.AddCommand(command);
            }
            return this;
        }

        public Shell ClearCommands()
        {
            lock (_lock)
            {
                _container = new ShellCommandsContainer();
            }
            return this;
        }

        #endregion

        protected virtual void OnShellCommandNotFound(string input, string[] tokens)
        {

            var handler = ShellCommandNotFound;

            if (handler != null)
            {
                handler.Invoke(this, new CommandNotFoundEventArgs(input, tokens));
            }
            else
            {
                Console.WriteLine("Command not found: {0}", input);
            }
        }

        protected virtual void OnPrintAlternatives(PrintAlternativesEventArgs e)
        {
            var handler = PrintAlternatives;
            if (handler != null)
            {
                handler(this, e);
            }
            else
            {
                Console.WriteLine("Possible completions:");
                foreach (var item in e.Alternatives)
                {
                    Console.WriteLine($"- {item}");
                }
            }
        }
    }
}
