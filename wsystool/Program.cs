using xayrga.byteglider;
using xayrga.cmdl;
using Newtonsoft.Json;
using System.Diagnostics;

namespace wsystool
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[]
            {
                "pack",
                "project",
                "new.wsys"
            };
#endif
            Console.WriteLine("WSYSTool - created by xayrga - http://github.com/xayrga/wsystool");
            cmdarg.cmdargs = args;

            var operation = cmdarg.assertArg(0, "Operation");
            operation = operation.ToLower();
            var awPath = cmdarg.findDynamicStringArgument("awpath", "Banks");

            Stopwatch TaskTimer = new Stopwatch();
            TaskTimer.Start();
            switch (operation)
            {
                case "unpack":
                    {
                        
                        var wsysFile = cmdarg.assertArg(1, "WSYS File");
                        var projectFolder = cmdarg.assertArg(2, "Project Folder");
                        var waveOut = cmdarg.findDynamicStringArgument("waveout", null);
                        var projFileName = Path.GetFileName(wsysFile);

                        cmdarg.assert(File.Exists(wsysFile), "Cannot locate WSYS file");

                        if (!Directory.Exists(projectFolder))
                            Directory.CreateDirectory(projectFolder);


                        var wsysHnd = File.OpenRead(wsysFile);
                        var wsysRd = new bgReader(wsysHnd);
                        var WSYS = WaveSystem.CreateFromStream(wsysRd);
                        var Serializer = new WSYSProjectDeserializer();

                        Console.WriteLine($"[wsystool] Loading {projFileName} ");
                        Serializer.LoadWSYS(WSYS, awPath);

                        Console.WriteLine($"[wsystool] {projFileName} Exporting project structure");
                        Serializer.SaveProjectData(WSYS, projectFolder);

                        if (waveOut != null)
                        {
                            if (!Directory.Exists(waveOut))
                                Directory.CreateDirectory(waveOut);
                            Console.WriteLine($"[wsystool] WAVEOUT: {projFileName} Extracting wave data...");
                            Serializer.RenderWaveData(waveOut);
                        }
                    }
                    break;
                case "pack":
                    {

                        var projectFile = cmdarg.assertArg(1, "WSYS Project Folder");
                        var outFile = cmdarg.assertArg(2, "WSYS File");

                        var Serializer = new WSYSProjectSerializer();
#if DEBUG
                        // it's fucking raw
                        var WSYS = Serializer.LoadProjectData(projectFile);
                        Serializer.WriteWaveSystem(outFile, awPath);
#endif
#if RELEASE
                        try
                        {
                            var WSYS = Serializer.LoadProjectData(projectFile);
                            Serializer.WriteWaveSystem(outFile, awPath);
                        } catch (WSYSProjectException ex)
                        {
                            var old = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(ex.Message);
                            Console.ForegroundColor = old;
                        }
#endif

                    }
                    break;
            }
#if DEBUG 

            Console.WriteLine($"Execution time {TaskTimer.Elapsed.TotalSeconds}s");
#endif


        }
    }
}