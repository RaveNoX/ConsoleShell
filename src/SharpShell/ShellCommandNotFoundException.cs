#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace SharpShell
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