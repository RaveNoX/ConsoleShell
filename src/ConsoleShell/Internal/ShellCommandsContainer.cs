#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace ConsoleShell.Internal
{
    internal class ShellCommandsContainer
    {
        private readonly SortedDictionary<string, object> _commandsTree;
        private readonly SortedDictionary<string, string> _commandsDescriptions;        

        public ShellCommandsContainer()
        {
            _commandsTree = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _commandsDescriptions = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);            
        }

        public SortedList<string, string> GetDescriptions(string prefix = null)
        {
            var ret = _commandsDescriptions.AsEnumerable();

            if (prefix != null)
            {
                ret = ret.Where(x => x.Key.StartsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return new SortedList<string, string>(ret.ToDictionary(x => x.Key, x => x.Value), StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> CompleteInput(Shell shell, string input)
        {
            var tokens = new Queue<string>(ShellCommandTokenizer.Tokenize(input));
            var endsWithSpace = input.EndsWith(" ");

            var result = FindMatchedCommands(tokens, endsWithSpace);

            if (result.Commands.Count == 1)
            {
                var command = result.Commands.First();

                if (tokens.Any() || (endsWithSpace && !result.IsTreeLevel))
                {
                    var commandValue = command.Value as IShellCommand;

                    if (commandValue != null)
                    {
                        var completeResult = commandValue.Complete(shell, tokens.ToArray()) ?? new string[] { };

                        if (!(completeResult.Length == 1 && completeResult[0] == tokens.LastOrDefault() && endsWithSpace))
                        {
                            return completeResult;
                        }
                    }

                    return new string[] { };
                }                

                return new[] { command.Key };
            }

            if (result.Commands.Count > 1 && !tokens.Any())
            {
                return result.Commands.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            return new string[] { };
        }
                
        public Action FindCommand(Shell shell, string[] tokens)
        {
            var tokensQueue = new Queue<string>(tokens);
            var result = FindMatchedCommands(tokensQueue);

            if (result.Commands.Count == 1)
            {
                var command = (result.Commands.First().Value as IShellCommand);
                if (command != null)
                {
                    return () =>
                    {
                        command.Invoke(shell, tokensQueue.ToArray());
                    };
                }
            }

            return null;
        }

        private FindMatchedResult FindMatchedCommands(Queue<string> tokens, bool fullMatch = false)
        {
            IDictionary<string, object> treeLevel = _commandsTree;

            while (tokens.Any())
            {
                var token = tokens.Dequeue();

                var matches = treeLevel
                    .Where(x => x.Key.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(x => x.Key, x => x.Value);

                if (matches.Count == 1)
                {
                    var match = matches.First();

                    if (match.Value is IShellCommand)
                    {
                        return new FindMatchedResult(new SortedDictionary<string, object>
                        {
                            {match.Key, match.Value}
                        });
                    }

                    if (!tokens.Any() && !fullMatch)
                    {
                        return new FindMatchedResult(new SortedDictionary<string, object>
                        {
                            {match.Key, match.Value}
                        });
                    }

                    treeLevel = match.Value as IDictionary<string, object>;

                    if (treeLevel == null)
                    {
                        throw new NullReferenceException("treeLevel must be not null");
                    }

                    continue;
                }

                return new FindMatchedResult(new SortedDictionary<string, object>(matches));
            }

            return new FindMatchedResult(new SortedDictionary<string, object>(treeLevel), true);
        }

        #region Commands operations

        public void AddCommand(IShellCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            TreeAdd(command);

            _commandsDescriptions.Add(command.Pattern, command.Description);
        }

        public void Clear()
        {
            _commandsTree.Clear();
            _commandsDescriptions.Clear();
        }

        #endregion

        #region Tree operations

        private void TreeAdd(IShellCommand command)
        {
            var tokens = new Queue<string>(ShellCommandTokenizer.Tokenize(command.Pattern));

            var treeLevel = _commandsTree;
            while (tokens.Any())
            {
                var token = tokens.Dequeue();

                object leaf;
                treeLevel.TryGetValue(token, out leaf);

                if (tokens.Count == 0)
                {
                    if (leaf == null)
                    {
                        leaf = command;
                        treeLevel.Add(token, leaf);
                    }

                    if (leaf != command)
                    {
                        throw new NotSupportedException("Already have handler for command");
                    }
                }
                else
                {
                    if (leaf == null)
                    {
                        leaf = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        treeLevel.Add(token, leaf);
                    }

                    treeLevel = leaf as SortedDictionary<string, object>;

                    if (treeLevel == null)
                    {
                        throw new InvalidOperationException("Tree leaf must be Dictionary");
                    }
                }
            }
        }

        #endregion

        private class FindMatchedResult
        {
            public FindMatchedResult(SortedDictionary<string, object> commands, bool isTreeLevel = false)
            {
                Commands = commands;
                IsTreeLevel = isTreeLevel;
            }

            public SortedDictionary<string, object> Commands { get; }
            public bool IsTreeLevel { get; }
        }
    }
}