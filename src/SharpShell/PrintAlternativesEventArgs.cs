namespace SharpShell
{
    public class PrintAlternativesEventArgs
    {
        public PrintAlternativesEventArgs(string[] alternatives)
        {
            Alternatives = alternatives;
        }

        public string[] Alternatives { get; }
    }
}