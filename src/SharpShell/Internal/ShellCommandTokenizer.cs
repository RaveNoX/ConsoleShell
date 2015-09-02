#region Usings

using System.Collections.Generic;
using System.Text;

#endregion

namespace SharpShell.Internal
{
    internal static class ShellCommandTokenizer
    {
        public static IEnumerable<string> Tokenize(string commandLine)
        {
            var ret = new List<string>();
            var arg = new StringBuilder();

            var t = commandLine.Length;

            for (var i = 0; i < t; i++)
            {
                var c = commandLine[i];

                switch (c)
                {
                    case '"':
                    case '\'':
                        var end = c;

                        for (i++; i < t; i++)
                        {
                            c = commandLine[i];

                            if (c == end)
                                break;
                            arg.Append(c);
                        }
                        break;
                    case ' ':
                        if (arg.Length > 0)
                        {
                            ret.Add(arg.ToString());                            
                            arg.Length = 0;
                        }
                        break;
                    default:
                        arg.Append(c);
                        break;
                }
            }

            if (arg.Length > 0)
            {
                ret.Add(arg.ToString());                
                arg.Length = 0;
            }

            return ret;
        }        
    }
}