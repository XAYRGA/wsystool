using JaiMaker;
using xayrga.byteglider;
using xayrga.cmdl;
using Newtonsoft.Json;

namespace wsystool2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            args = new string[]
            {
                "unpack",
                "0.wsy",
                "project",
                "-awpath",
                "Banks",
                "-waveout",
                "project/waveout"
            };
            cmdarg.cmdargs = args;

            var operation = cmdarg.assertArg(0, "Operation");
            operation = operation.ToLower();
            switch (operation)
            {
                case "unpack":
                    var wsysFile = cmdarg.assertArg(1, "WSYS File");
                    var projectFolder = cmdarg.assertArg(2, "Project Folder");
                    var awPath = cmdarg.findDynamicStringArgument("awpath", "Banks");
                    var waveOut = cmdarg.findDynamicStringArgument("waveout", null);

                    cmdarg.assert(File.Exists(wsysFile), "Cannot locate WSYS file");        
                    
                    if (!Directory.Exists(projectFolder))
                        Directory.CreateDirectory(projectFolder);


                    var wsysHnd = File.OpenRead(wsysFile);
                    var wsysRd = new bgReader(wsysHnd);
                    var WSYS = WaveSystem.CreateFromStream(wsysRd);
                    var Serializer = new WSYSSerializer();

                    Serializer.LoadWSYS(WSYS,awPath);

                    if (waveOut != null)
                    {
                        if (!Directory.Exists(waveOut))
                            Directory.CreateDirectory(waveOut);
                        Serializer.RenderWaveData(waveOut);
                    }

                    Serializer.SaveProjectData(WSYS,projectFolder);
                    break;
            }


        }
    }
}