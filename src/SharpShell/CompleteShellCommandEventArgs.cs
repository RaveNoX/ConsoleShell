using System;

namespace SharpShell
{
    public class CompleteShellCommandEventArgs:EventArgs
    {
        public CompleteShellCommandEventArgs(Shell shell, string[] tokens)
        {
            Shell = shell;
            Tokens = tokens;            
        }


        public Shell Shell { get; }
        public string[] Tokens { get; }
        public string[] Result { get; set; }
    }
}