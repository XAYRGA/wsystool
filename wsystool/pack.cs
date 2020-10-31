using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;
using libJAudio; 

namespace wsystool
{
    public static class pack
    {

        public static Stream[] awHandles;

        public static string createProjectFolder(string output_file)
        {
            var path = Path.GetFullPath(output_file);
            var dir = Path.GetDirectoryName(path);
            var fname = Path.GetFileName(output_file);
            Directory.CreateDirectory($"{dir}/aw_{fname}");
            return $"{dir}/aw_{fname}";
        }

        public static unsafe void pack_do(string projFolder, string outfile)
        {
            cmdarg.assert(!Directory.Exists(projFolder), "Project folder '{0}' could not be found", projFolder);
            cmdarg.assert(!File.Exists($"{projFolder}/project.wso"), "Could not find 'project.wso'") ;
            string awOutput = null;
            try
            {
                awOutput = createProjectFolder(outfile);
            } catch (Exception E) { cmdarg.assert($"Could not create export folder. ({E.Message})"); }


            FileStream wsFile = null;
            BeBinaryWriter wsysWrite = null;
            try
            {
                wsFile = File.Open($"{projFolder}/project.wso",FileMode.Open,FileAccess.ReadWrite);
                wsysWrite = new BeBinaryWriter(wsFile);
            }
            catch (Exception E) { cmdarg.assert($"Could not open 'project.wso' ({E.Message})"); }

            var wsLoader = new libJAudio.Loaders.JA_WSYSLoader_V1();
            var wsReader = new BeBinaryReader(wsFile);
            JWaveSystem WaveSystem = wsLoader.loadWSYS(wsReader, 0x00000000);
            awHandles = new Stream[WaveSystem.Groups.Length];

            for (int cWI = 0; cWI < WaveSystem.Groups.Length; cWI++)
            {
                var cGrp = WaveSystem.Groups[cWI];
                var cScn = WaveSystem.Scenes[cWI];
                var awOutHnd = File.Open($"{awOutput}/{cGrp.awFile}",FileMode.OpenOrCreate,FileAccess.ReadWrite);
               // var awWriter = new BeBinaryWriter(awOutHnd);
                var total_aw_offset = 0;
                for (int i=0; i < cGrp.Waves.Length; i++)
                {
                    var cData = cScn.CDFData[i];
                    var wData = cGrp.Waves[i];
                    byte[] adpcm_data;

                    var cWaveFile = $"{projFolder}/custom/{cData.waveid}.wav";
                    if (File.Exists(cWaveFile))
                    {

                        var strm = File.OpenRead(cWaveFile);
                        var strmInt = new BinaryReader(strm);

                        var WaveData = PCM16WAV.readStream(strmInt);

                        if (WaveData == null)
                            cmdarg.assert($"ABORT: '{cWaveFile} has invalid format.");

                        Console.WriteLine($"Packing custom wave custom/{cData.waveid}.wav");
                        cmdarg.assert(WaveData.sampleRate > 32000, $"ABORT: '{cWaveFile} has samplerate {WaveData.sampleRate}hz (Max: 32000hz)");

                        adpcm_data = new byte[((WaveData.sampleCount / 9) * 16)];
                        var adp_f_pos = 0;
                        var hist0 = 0;
                        var hist1 = 0;
                        var wavFP = WaveData.buffer;
                        for (int ix = 0; ix < WaveData.sampleCount; ix += 16)
                        {
                            short[] wavIn = new short[16];
                            byte[] adpcmOut = new byte[9];
                            for (int k = 0; k < 16; k++)
                            {
                                wavIn[k] = wavFP[ix + k];
                            }
                            bananapeel.Pcm16toAdpcm4(wavIn, adpcmOut, ref hist0, ref hist1);
                            for (int k = 0; k < 9; k++)
                            {
                                adpcm_data[adp_f_pos] = adpcmOut[k];
                                adp_f_pos++;
                            }
                        }
                        /*
                        for (int k = 0; k < 9; k++)
                        {
                            adpcm_data[(adp_f_pos - 9) + k] = 0; // what the fuck what the fuck what the FUCK??? // Last sample is cuck data? 
                            adp_f_pos++;
                        }
                        */

                        wData.sampleRate = WaveData.sampleRate;
                        wData.sampleCount = WaveData.sampleCount;

                        if (WaveData.sampler.loops !=null && WaveData.sampler.loops.Length > 0)
                        {
                            wData.loop = true;
                            wData.loop_start = WaveData.sampler.loops[0].dwStart;
                            wData.loop_end = WaveData.sampler.loops[0].dwEnd;
                        }
                    } else
                    {
                        var reffile = $"{projFolder}/ref/{cData.waveid}.adp";
                        cmdarg.assert(!File.Exists(reffile), "ABORT: Could not find reference file: {0}", reffile);
                        adpcm_data = File.ReadAllBytes(reffile);
                    }

                    wsysWrite.BaseStream.Position = wData.mOffset;
                    wsysWrite.Seek(0x04, SeekOrigin.Current);
                    wsysWrite.Write((float)wData.sampleRate);
                    wsysWrite.Write(total_aw_offset);
                    wsysWrite.Write(adpcm_data.Length);
                    wsysWrite.Flush();
                    awOutHnd.Write(adpcm_data, 0, adpcm_data.Length);
                    awOutHnd.Flush();
                    total_aw_offset += adpcm_data.Length;
                    util.consoleProgress($"\t->Rebuild ({cGrp.awFile})", i, cGrp.Waves.Length - 1,true);
                }
                awOutHnd.Close();
                Console.WriteLine();
            }
            wsysWrite.Close();
            Console.Write($"Writing {outfile}...");
            File.Copy($"{projFolder}/project.wso", outfile,true);
            Console.WriteLine("OK!");
        }
    }
}
