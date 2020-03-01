using System;

namespace AhornPluginMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Ahorn Plugin Maker v1.0");
            Console.WriteLine("Give the path to a .cs file to turn it into an Ahorn Plugin");
            string path = Console.ReadLine().Trim('"');
            Console.WriteLine();
            AhornPlugin plugin;
            try
            {
                plugin = new AhornPlugin(path);
            } catch (Exception e)
            {
                Console.WriteLine($"Ahorn Plugin Maker encountered an error!\n {e.Message}\n {e.StackTrace}\n");
                PrintPossibleErrors(true);
                Console.WriteLine("If none of the above are true, report the bug to JaThePlayer#2580 on Discord, together with your entity's CustomEntity attribute and constructor");
                Console.ReadLine();
                return;
            }
            if (plugin.Type == AhornPlugin.PlacementTypes.Entity)
                Console.WriteLine("\n\nRemember to change the \"sprite\" variable to point to your entity's sprite");
            Console.Write("The plugin isn't working? ");
            PrintPossibleErrors(false, plugin);
            Console.ReadLine();
        }

        static void PrintPossibleErrors(bool crashed, AhornPlugin plugin = null)
        {
            Console.WriteLine("Here's some things that might've gone wrong:");
            Console.WriteLine("- Your entity had more than 1 constructor");
            Console.WriteLine("- Your entity didn't have a CustomEntity attribute");
            if (!crashed)
            {
                if (plugin != null && plugin.Type == AhornPlugin.PlacementTypes.Entity)
                Console.WriteLine("- You didn't change the \"sprite\" variable in the ahorn plugin or the path is incorrect");
            }
                
        }
    }
}
