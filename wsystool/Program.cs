using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wsystool
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG

#endif
            crc32.reset(); // Initialize + preseed CRC32; 
            cmdarg.cmdargs = args;



            /*
            var fw = File.OpenRead("bup4.wav");
            var w = PCM16WAV.readStream(new BinaryReader(fw));
            for (int i=0; i < w.cuePoints.Length; i++)
            {
                Console.WriteLine(w.cuePoints[i].frameOffset);
            }
            ObjectDumper.Dumper.Dump(w, "WAVE", Console.Out);
            Console.WriteLine(w);
            var ww = File.OpenWrite("yes4.wav");
            var bw = new BinaryWriter(ww);
            w.writeStreamLazy(bw);
            bw.Flush();
            bw.Close();
            Console.ReadLine();
            Environment.Exit(0);
            */


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
                    Console.WriteLine($"Unknown operation '{operation}'. See 'wsystool help'");
                    break;
            }
       

        }
    }
}
