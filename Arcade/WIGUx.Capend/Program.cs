using System;

class Program
{
    const string name = "Capend";
    const string version = "1.2";
    static void Main(string[] args)
    {
#if DEBUG
        //var process = Process.Start("notepad");
        //args = new string[] { "1", "51424", "esto es la bomba" };
        //args = new string[] { "0", process.Id.ToString() };
        args = new string[] { "--version" };
#endif

        // === Add MAMEHOOK & STANDALONEMAMEHOOK Modes Here ===
        if (args.Length > 0)
        {
            string arg = args[0].ToLowerInvariant();
            if (arg == "mamehook")
            {
                Capend.MameHookEntry.RunMameHook(true); // EMUVR mode
                return;
            }
            else if (arg == "standalonemamehook")
            {
                Capend.MameHookEntry.RunMameHook(false); // Standalone mode
                return;
            }
        }
        // === End Modes ===

        if (args.Length < 2)
        {
            var arg = string.Join(" ", args).ToLower();
            if (arg.Contains("--version") || arg.Contains("-v"))
            {
                // Cambiar color para el nombre
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{name}");

                // Cambiar color para la versión
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($" v{version}");

                // Cambiar color para el equipo
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(" by WIGU");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("x");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(" team");

                // Restablecer el color de la consola
                Console.ResetColor();
            }
            ;
            return;
        }

        int operacion;
        if (!int.TryParse(args[0], out operacion))
        {
            //Console.WriteLine("El primer parámetro debe ser un entero (1 para eliminar, 2 para renombrar).");
            return;
        }

        switch (operacion)
        {
            case 0:
                KeyPressHelper.SimulateEscKeyPress();
                ProcessHelper.RemoveChildProcess(args[1]);
                break;
            case 1:
                WindowHelper.RenameWindow(args[1], args[2]);
                break;
            default:
                break;
        }
    }
}
