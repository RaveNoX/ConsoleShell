#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace ConsoleShell
{
    public class ShellCommandNotFoundException : Exception
    {
        public ShellCommandNotFoundException(IEnumerable<string> tokens)
        {
            Tokens = tokens;
        }
        
        public IEnumerable<string> Tokens { get; }
    }
}