using System;

namespace SharpShell
{
    public class InvokeShellCommandEventArgs:EventArgs
    {
        public InvokeShellCommandEventArgs(Shell shell, string[] arguments)
        {
            Shell = shell;
            Arguments = arguments;
        }

        public Shell Shell { get; }
        public string[] Arguments { get; }
    }
}