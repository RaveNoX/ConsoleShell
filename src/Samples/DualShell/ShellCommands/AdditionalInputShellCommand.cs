#region Usings

using System;
using ConsoleShell;

#endregion

namespace DualShell.ShellCommands
{
    internal class AdditionalInputShellCommand : ShellCommandBase
    {
        public AdditionalInputShellCommand() : base("input add", "Additional input test command")
        {
        }        

        public override void Invoke(Shell shell, string[] args)
        {                        
            var model = new InputModel();
            Console.Write("Name: ");
            model.Name = Console.ReadLine();

            Console.Write("Surname: ");
            model.Surname = Console.ReadLine();

            Console.WriteLine();
            Console.WriteLine("Hello {0} {1}!", model.Name, model.Surname);            
        }

        private class InputModel
        {
            public string Name { get; set; }
            public string Surname { get; set; }
        }
    }
}