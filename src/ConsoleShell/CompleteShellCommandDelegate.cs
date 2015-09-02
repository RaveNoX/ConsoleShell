namespace ConsoleShell
{
    public delegate string[] CompleteShellCommandDelegate(Shell shell, IShellCommand command, string[] tokens);
}