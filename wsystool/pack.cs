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

        public static byte[] transform_pcm16_mono_adpcm(PCM16WAV WaveData, out int adjustedSampleCount)
        {
            //adpcm_data = new byte[((WaveData.sampleCount / 9) * 16)];

          
            int frameCount = (WaveData.sampleCount + 16 - 1) / 16;
            adjustedSampleCount = frameCount * 16; // now that we have a properly calculated frame count, we know the amount of samples that realistically fit into that buffer. 
            var frameBufferSize = frameCount * 9; // and we know the amount of bytes that the buffer will take.
            var adjustedFrameBufferSize = frameBufferSize; //+ (frameBufferSize % 32); // pads buffer to 32 bytes. 
            byte[] adpcm_data = new byte[adjustedFrameBufferSize]; // 9 bytes per 16 samples

            //Console.WriteLine($"\n\n\n{WaveData.sampleCount} samples\n{frameCount} frames.\n{frameBufferSize} bytes\n{adjustedFrameBufferSize} padded bytes. ");
            var adp_f_pos = 0; // ADPCM position

            var lastSample = 0; 
            var penultimate = 0; 

            var wavFP = WaveData.buffer;
            // transform one frame at a time
            for (int ix = 0; ix < frameCount; ix++)
            {
                short[] wavIn = new short[16];
                byte[] adpcmOut = new byte[9];
                for (int k = 0; k < 16; k++)
                {
                    if ( ((ix*16) + k) >= WaveData.sampleCount)
                        continue; // skip if we're out of samplebuffer, continue to build last frame
                    wavIn[k] = wavFP[ (ix*16) + k];
                }
                // build ADPCM frame
                bananapeel.Pcm16toAdpcm4(wavIn, adpcmOut, ref lastSample, ref penultimate); // convert PCM16 -> ADPCM4
                for (int k = 0; k < 9; k++)
                {
                    adpcm_data[adp_f_pos] = adpcmOut[k]; // dump into ADPCM buffer.
                    adp_f_pos++; // increment ADPCM byte
                }
            }
            return adpcm_data;
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
                var awOutWt = new BeBinaryWriter(awOutHnd);
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

                        cmdarg.assert(WaveData.sampleRate > 48000, $"ABORT: '{cWaveFile}' has samplerate {WaveData.sampleRate}hz (Max: 32000hz)");
                        cmdarg.assert(WaveData.channels > 1, $"ABORT: '{cWaveFile}' has too many channels {WaveData.channels}chn (Max: 1)");
                        Console.WriteLine($"\n\t*** Packing custom wave {cWaveFile}");
                        int samplesCount = 0;
                        var byteInfo = transform_pcm16_mono_adpcm(WaveData, out samplesCount);
                        adpcm_data = byteInfo;
                        wData.sampleRate = WaveData.sampleRate;
                        wData.sampleCount = samplesCount; // __wow__

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
                    /*
                     

                       0x00 - byte unknown
                        0x01 - byte format
                        0x02 - byte baseKey  
                        0x03 - byte unknown 
                        0x04 - float sampleRate 
                        0x08 - int32 start
                        0x0C - int32 length 
                        0x10 - int32 loop >  0  ?  true : false 
                        0x14 - int32 loop_start
                        0x18 - int32 loop_end 
                        0x1C  - int32 sampleCount 

                */             
                //Console.WriteLine()
                    wsysWrite.BaseStream.Position = wData.mOffset;
                    wsysWrite.Seek(0x04, SeekOrigin.Current);
                    wsysWrite.Write((float)wData.sampleRate);
                    wsysWrite.Write(total_aw_offset);
                    wsysWrite.Write(adpcm_data.Length); // SHOULD spit out exact frame length. holy FUCK.
                    wsysWrite.Write(wData.loop ? 0xFFFFFFFF : 0x00000000);
                    wsysWrite.Write(wData.loop_start);
                    wsysWrite.Write(wData.loop_end);
                    wsysWrite.Write(wData.sampleCount);
                    wsysWrite.Flush();

                    // write buffer to AW
                    awOutHnd.Write(adpcm_data, 0, adpcm_data.Length);
                    //util.padTo(awOutWt, 32); // pad to 32 bytes go to hell
                    awOutHnd.Flush();
                    total_aw_offset = (int)awOutHnd.Position; // write the out position because that's what we've padded to

                    if (total_aw_offset % 9 != 0)
                    {
                        var oc = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"ALERT: ADPCM SAMPLES NOT ALIGNED TO 9 Dividend remainder: ({total_aw_offset % 9})");
                        Console.ForegroundColor = oc;
                    }
                    // You can't see me
                    // behind the screen
                    util.consoleProgress($"\t->Rebuild ({cGrp.awFile})", i, cGrp.Waves.Length - 1,true);
                    /// I'm half human
                    /// and half M A C H I N E                   
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
