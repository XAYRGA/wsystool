using System;
using System.Collections.Generic;
using System.Text;

namespace wsystool
{
    public static class HelpManifest
    {
        public static void print_general()
        {
            Console.WriteLine("wsystool (C) XAYRGA 2020");
            Console.WriteLine("Syntax: ");
            Console.WriteLine("wsystool <operation> [args....]");
            Console.WriteLine("wsystool unpack <wsysfile> <projectfolder> [-awpath path_to_.aw_banks]");
            Console.WriteLine("wsystool unpack <projectfolder> <wsysfile>");
        }

    }
}
