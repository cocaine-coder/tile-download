using System;

namespace TileDownload.CLI.Minify
{
    internal static class LogHelper
    {
        public static void LogHeader(string log)
        {
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>> {log} <<<<<<<<<<<<<<<<<<<");
        }

        public static void LogInfo(string log = "")
        {
            Console.WriteLine(log);
        }

        public static void Exit(params string[] logs)
        {
            foreach (var log in logs)
            {
                Console.WriteLine(log);
            }

            Environment.Exit(1);
        }
    }
}
