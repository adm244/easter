using System;
using System.IO;

using EasterLib;

namespace Easter
{
    class Program
    {
        static void Main(string[] args)
        {
            //Package.Unpack(args[0]);

            string[] archives = Directory.GetFiles(".", "*.g");
            for (int i = 0; i < archives.Length; ++i)
            {
                Package.Unpack(archives[i]);
            }
        }
    }
}
