﻿using System;

namespace SharpShell
{
    public class CommandNotFoundEventArgs:EventArgs
    {
        public CommandNotFoundEventArgs(string input, string[] tokens)
        {
            Input = input;
            Tokens = tokens;
        }

        public string Input { get; }
        public string[] Tokens { get; }
    }
}
