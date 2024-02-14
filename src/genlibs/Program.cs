using System.IO;
using System;
using CppSharp;

namespace genlibs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("WORK DIR: {0}", Environment.CurrentDirectory);

            Console.WriteLine("****** Generate external library wrappers ******");

            Console.Write("RSP Library ... ");
            ConsoleDriver.Run(new RspLibrary());
            Console.WriteLine("Done!");

            Console.Write("GLide64 Library ... ");
            ConsoleDriver.Run(new Glide64Library());
            Console.WriteLine("Done!");
        }
    }
}
