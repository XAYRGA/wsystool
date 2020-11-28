using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;
using libJAudio;
using Newtonsoft.Json;

namespace wsysbuilder
{

    public static class unpack
    {
        public static uint[] hashTable = new uint[0xFFFF];
        public static bool[] wavHash = new bool[0xFFFF];

        public static Stream[] awHandles;

        private static Dictionary<int, JWaveDescriptor> waveTable = new Dictionary<int, JWaveDescriptor>();

        public static void createProjectFolder(string path)
        {
            Directory.CreateDirectory(path);
            Directory.CreateDirectory($"{path}/ref");
            Directory.CreateDirectory($"{path}/wav");
            Directory.CreateDirectory($"{path}/custom");
            Directory.CreateDirectory($"{path}/scenes");
            File.WriteAllText($"{path}/ref/__DO_NOT_TOUCH_THESE_FILES.txt", "Don't touch these files, they are used to reference the rebuild data.");
            File.WriteAllText($"{path}/custom/__PUT_WAV_FILES_IN_HERE.txt", "These files will overwrite the ones in the game when the ws is rebuilt.");
            File.WriteAllText($"{path}/wav/__README.txt", "These files aren't used for anything other than listening.\nModifying these will have no affect on the game.\nIf you modify one of these, move it to the 'rep' folder for it to take place in game.");
            File.WriteAllText($"{path}/__README.txt", "Don't manually modify the .wso file, it's a running copy of the WS in your current project state, and is used to rebuild the WSdata\n\nRead the readme's in the other folder.");
        }

        private static Stream getAWFileHandle(string basen,string name)
        {
           // Console.WriteLine($"./{basen}/{name}");
            if (File.Exists($"./{basen}/{name}"))
                return File.OpenRead($"./{basen}/{name}");
            if (File.Exists($"./{basen}/Waves/{name}"))
                return File.OpenRead($"./{basen}/Waves/{name}");
            if (File.Exists($"./{basen}/Banks/{name}"))
                return File.OpenRead($"/{basen}/Banks/{name}");
            if (File.Exists(name))
                return File.OpenRead(name);
            var w = cmdarg.findDynamicStringArgument("-awpath", null);
            //Console.WriteLine($"{w}/{name}");
            if (File.Exists($"{w}/{name}"))
                return File.OpenRead($"{w}/{name}");
            return null;
        }


        public static short[] ADPCM42PCM16(byte[] adpdata)
        {
            var totalSamples = ( (adpdata.Length / 9) * 16) ; /// ADPCM Frames are 9 bytes length, comes out to 16 short samples. 
            var frameOffset = 0;  // Initialize 0 frame offset
            short[] smplBuff = new short[totalSamples]; // Initialize container for samples
            int pen = 0; // penultimate
            int last = 0; // last sample
            for (int sam = 0; sam < totalSamples; sam+=16) // Increment samples in blocks of 16
            {
                byte[] adpcm4 = new byte[9]; // 9 ADPCM frame
                short[] sample = new short[16]; // 16 PCM frame
                for (int i = 0; i < 9; i++) // sprawl out ADPCM Frame
                    adpcm4[i] = adpdata[frameOffset + i]; // ^
                frameOffset += 9; // Increment to next frame (0 indexed)
                bananapeel.Adpcm4toPcm16(adpcm4, sample, ref last, ref pen); // transform, store in "sample"
                for (int i = 0; i < 16; i++)  // sprawl out PCM sample into sample buffer
                    smplBuff[sam + i] = sample[i]; // ^
            }
            return smplBuff; // return
        }



        public static short[] PCM8216(byte[] adpdata)
        {          
            short[] smplBuff = new short[adpdata.Length]; // Initialize container for samples
            for (int sam = 0; sam < adpdata.Length; sam++) // Increment samples in blocks of 16
            {
                smplBuff[sam] = (short)(adpdata[sam] * (adpdata[sam] < 0 ? 256 : 258));  
            }
            return smplBuff; // return
        }

        public unsafe static short[] PCM16ByteToShort(byte[] pcm)
        {
            //var pcmS = new short[ (pcm.Length / 2) + 1];
            var pcmS = new short[(pcm.Length + 2 - 1) / 2];

            fixed (byte* pcmD = pcm)
            {
                var pcmBy = (short*)pcmD;
                //for (int i=0; i < pcm.Length;i++)
                for (int i = 0; i < pcmS.Length; i++)
                    {
                    pcmS[i] = pcmBy[i];
                }
            }
            return pcmS;
        }


        public static unsafe void unpack_do(string filename, string projFolder)
        {
            cmdarg.assert(!File.Exists(filename), "The file {0} could not be found", filename);
            try
            {
                createProjectFolder(projFolder);
            } catch (Exception E) { cmdarg.assert($"Could not create project folder. ({E.Message})"); }


            byte[] wsData = null;
            MemoryStream wsFile = null;
            try
            {
                wsData = File.ReadAllBytes(filename);
                wsFile = new MemoryStream(wsData);
            }
            catch (Exception E) { cmdarg.assert($"Could not open WSYS ({E.Message})"); }

            var base_addr = Path.GetDirectoryName(filename);
            var wsLoader = new libJAudio.Loaders.JA_WSYSLoader_V1();
            var wsReader = new BeBinaryReader(wsFile);
            JWaveSystem WaveSystem = wsLoader.loadWSYS(wsReader, 0x00000000);
            awHandles = new Stream[WaveSystem.Groups.Length];
            var WSP = new wsysProject();
            WSP.sceneOrder = new string[WaveSystem.Groups.Length];
            WSP.id = WaveSystem.id;
            WSP.waveTable = "wavetable.json";
            for (int cWI = 0; cWI < WaveSystem.Groups.Length; cWI++)
            {
                var cGrp = WaveSystem.Groups[cWI];
                var cScn = WaveSystem.Scenes[cWI];

                var awf = getAWFileHandle(base_addr, cGrp.awFile);
                if (awf == null)
                    cmdarg.assert("Cannot find AWFile {0}", cGrp.awFile);

                var cGrpIDs = new int[cScn.CDFData.Length];

                for (int i = 0; i < cScn.CDFData.Length; i++)
                    cGrpIDs[i] = cScn.CDFData[i].waveid;
                var ms = new minifiedScene()
                {
                    waves = cGrpIDs,
                    awfile = cGrp.awFile,
                };

                var wbx = JsonConvert.SerializeObject(ms, Formatting.Indented);
                File.WriteAllText($"{projFolder}/scenes/{cGrp.awFile}.json",wbx);
                WSP.sceneOrder[cWI] = $"scenes/{cGrp.awFile}.json";


                for (int i=0; i < cGrp.Waves.Length; i++)
                {
                    var cData = cScn.CDFData[i];
                    var wData = cGrp.Waves[i];
                    var br = new BeBinaryReader(awf);
                    br.BaseStream.Position = wData.wsys_start;
                    var dat = br.ReadBytes(wData.wsys_size);
                    var crc = crc32.ComputeChecksum(dat);
                    if (hashTable[cData.waveid]!=0 && hashTable[cData.waveid]!=crc)
                        Console.WriteLine($"\nWARNING: {cData.waveid} in {cData.awid} ({cGrp.awFile}) had CRC {crc:X} when previous instance had {hashTable[cData.waveid]:X} -- sound data will be asymmetrical or broken in some areas.");
                    else if (hashTable[cData.waveid] != 0)
                    {
                        // Effectively, skip doing anything becuase the sound already exists. 
                    } else 
                    {
                        hashTable[cData.waveid] = crc;
                        File.WriteAllBytes($"{projFolder}/ref/{cData.waveid}.adp",dat);
                    }
                    util.consoleProgress($"\t->Extract ({cGrp.awFile})", i, cGrp.Waves.Length - 1,true);
                }
                Console.WriteLine();
            }


            if (!cmdarg.findDynamicFlagArgument("-skip-transform"))
            {
                Console.WriteLine("Transforming ADPCM data.... (may take a while)");
                for (int cWI = 0; cWI < WaveSystem.Groups.Length; cWI++)
                {
                    var cGrp = WaveSystem.Groups[cWI];
                    var cScn = WaveSystem.Scenes[cWI];
                    var awf = getAWFileHandle(base_addr, cGrp.awFile);
                    if (awf == null)
                        cmdarg.assert("Cannot find AWFile {0}", cGrp.awFile);
                    for (int i = 0; i < cGrp.Waves.Length; i++)
                    {
                        var cData = cScn.CDFData[i];
                        var wData = cGrp.Waves[i];
                        var br = new BeBinaryReader(awf);
                        br.BaseStream.Position = wData.wsys_start;
                        var dat = br.ReadBytes(wData.wsys_size);
                        if (wavHash[cData.waveid] == false) // skip writes if sound already exists.
                        {
                            //var crc = crc32.ComputeChecksum(dat);

                            waveTable[cData.waveid] = wData;

                            var pcmFinal = new short[0];
                            switch (wData.format)
                            {
                                case 0: // ADPCM4
                                    pcmFinal = ADPCM42PCM16(dat);
                                    break;
                                case 1:
                                    cmdarg.assert("ADPCM2 format is currently not supported.");
                                    break;
                                case 2: // PCM8
                                    pcmFinal = PCM8216(dat);
                                    break;
                                case 3: // PCM16
                                    pcmFinal = PCM16ByteToShort(dat);
                                    break;
                                default:
                                    cmdarg.assert($"Unknown decode format {wData.format}");
                                    break;
                            }

                            var nwf = new PCM16WAV()
                            {
                                format = 1,
                                sampleRate = (int)wData.sampleRate,
                                channels = 1,
                                blockAlign = 2,
                                bitsPerSample = 16,
                                buffer = pcmFinal,
                            };

                            if (wData.loop == true)
                            {
                                nwf.sampler.loops = new SampleLoop[1];
                                nwf.sampler.loops[0] = new SampleLoop()
                                {
                                    dwIdentifier = 0,
                                    dwEnd = wData.loop_end,
                                    dwFraction = 0,
                                    dwPlayCount = 0,
                                    dwStart = wData.loop_start,
                                    dwType = 0
                                };
                            }

                            var fileData = File.OpenWrite($"{projFolder}/wav/{cData.waveid}.wav");
                            var fileWriter = new BinaryWriter(fileData);
                            nwf.writeStreamLazy(fileWriter);
                            fileWriter.Flush();
                            fileData.Close();

                            wavHash[cData.waveid] = true;
                        }
                        util.consoleProgress($"\t->Transform ({cGrp.awFile})", i, cGrp.Waves.Length - 1, true);

                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine("Writing wavetable...");

            var wb = JsonConvert.SerializeObject(waveTable, Formatting.Indented);
            File.WriteAllText($"{projFolder}/wavetable.json", wb);

            Console.WriteLine("Writing Project File");

            wb = JsonConvert.SerializeObject(WSP, Formatting.Indented);
            File.WriteAllText($"{projFolder}/manifest.json", wb);
        }
    }
}
