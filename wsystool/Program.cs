using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wsysbuilder
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("!WSYSTOOL build in debug mode, do not push into release!");
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.ForegroundColor = ConsoleColor.Gray;
#endif
            crc32.reset(); // Initialize + preseed CRC32; 
            cmdarg.cmdargs = args;

            //Console.ReadLine();
            Console.WriteLine("wsystool JAudio WSYS packer / unpacker");
            //util.consoleProgress("Test", 50, 100);
            //Console.ReadLine();
            var operation = cmdarg.assertArg(0, "operation");
            switch (operation)
            {
                case "unpack":
                    var wsFile = cmdarg.assertArg(1, "WSYSFile");
                    var projectFolder = cmdarg.assertArg(2, "ProjectFolder");
                    unpack.unpack_do(wsFile,projectFolder);
                    break;
                case "pack":
                    var projectFile = cmdarg.assertArg(1, "WSYSProjectFolder");
                    var outFile = cmdarg.assertArg(2, "OutFile");
                    pack.pack_do(projectFile,outFile);
                    break;
                case "help":
                    HelpManifest.print_general();
                    break;
                default:
                    Console.WriteLine($"Unknown operation '{operation}'. See 'wsysbuilder help'");
                    break;
            }

        }
    }
}
