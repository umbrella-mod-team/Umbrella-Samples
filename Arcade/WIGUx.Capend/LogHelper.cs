
using System;
using System.IO;

static class LogHelper
{
    static string logFile;

    static bool WritesInFile = true;

    static LogHelper()
    {
        var location = typeof(Program).Assembly.Location;
        string currentDirectory = Path.GetDirectoryName(location);
        string nombre = Path.GetFileNameWithoutExtension(location);
        logFile = Path.Combine(currentDirectory, $"{nombre}.log");
        WritesInFile = File.Exists(logFile);
    }

    public static void Debug(string message)
    {
        if (WritesInFile)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(logFile, true))
                {
                    sw.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error al escribir en el archivo de log: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}
