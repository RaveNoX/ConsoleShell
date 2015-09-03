using System;

namespace ConsoleShell
{
    public class CommandNotFoundEventArgs:EventArgs
    {
        public CommandNotFoundEventArgs(string input)
        {
            Input = input;            
        }

        public string Input { get; }        
    }
}
