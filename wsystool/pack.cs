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

    internal struct wavePositionDescriptor
    {
        public int start;
        public int stop;
    }
    public static class pack
    {

        public static Stream[] awHandles;

        public static string createProjectFolder(string output_file)
        {
            var path = Path.GetFullPath(output_file);
            var dir = Path.GetDirectoryName(path);
            var fname = Path.GetFileName(output_file);
            try
            {
                Directory.Delete($"{dir}/aw_{fname}", true);
            }
            catch { }// whatever
            Directory.CreateDirectory($"{dir}/aw_{fname}");
            return $"{dir}/aw_{fname}";
        }

        public static void assertHalt(bool cond, string err)
        {
            if (cond == false)
                return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err);
            Console.Beep(1500,500);
   
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static void assertHalt(bool cond, string err, params object[] ob)
        {
            if (cond == false)
                return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err, ob);
            Console.Beep(1500, 500);
            Console.ReadLine();
            Environment.Exit(0);
        }


        public static void assertHaltStr(string err, params object[] ob)
        {

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err, ob);
            Console.Beep(1500, 500);
            Console.ReadLine();
            Environment.Exit(0);
        }
        public static byte[] transform_pcm16_mono_adpcm(PCM16WAV WaveData, out int adjustedSampleCount)
        {
            int last = 0;
            int penult = 0;
            return transform_pcm16_mono_adpcm(WaveData, out adjustedSampleCount, 0, out last,out penult);
        }
        public static byte[] transform_pcm16_mono_adpcm(PCM16WAV WaveData, out int adjustedSampleCount, int loopRetSample , out int last, out int penult)
        {
            //adpcm_data = new byte[((WaveData.sampleCount / 9) * 16)];

          
            int frameCount = (WaveData.sampleCount + 16 - 1) / 16;
            adjustedSampleCount = frameCount * 16; // now that we have a properly calculated frame count, we know the amount of samples that realistically fit into that buffer. 
            var frameBufferSize = frameCount * 9; // and we know the amount of bytes that the buffer will take.
            var adjustedFrameBufferSize = frameBufferSize; //+ (frameBufferSize % 32); // pads buffer to 32 bytes. 
            byte[] adpcm_data = new byte[adjustedFrameBufferSize + 18]; // 9 bytes per 16 samples 

            last = 0;
            penult = 0;
            int absolute = 0;
            int pcmLast = 0;
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
                    if ((ix * 16) + k == loopRetSample)
                    {
                        last = lastSample;
                        penult = penultimate;
                    }

                    wavIn[k] = wavFP[ (ix*16) + k];
                }

                // build ADPCM frame
                bananapeel.Pcm16toAdpcm4(wavIn, adpcmOut, ref lastSample, ref penultimate); // convert PCM16 -> ADPCM4
                //banan_brawl.encode_adpcm_16_managed(wavIn, adpcmOut, ref lastSample, ref penultimate, ref pcmLast);
                //banan_brawl.encode_adpcm_16_managed(wavIn, adpcmOut);
                for (int k = 0; k < 9; k++)
                {
                    adpcm_data[adp_f_pos] = adpcmOut[k]; // dump into ADPCM buffer.
                    //Console.WriteLine(adpcmOut[k]);
                    adp_f_pos++; // increment ADPCM byte
                }
            }
            return adpcm_data;
        }

        public static byte[] transform_pcm16_mono_adpcm4_hle(PCM16WAV WaveData, out int adjustedSampleCount, int loopRetSample, out int last, out int penult)
        {
            //adpcm_data = new byte[((WaveData.sampleCount / 9) * 16)];


            int frameCount = (WaveData.sampleCount + 16 - 1) / 16;
            adjustedSampleCount = frameCount * 16; // now that we have a properly calculated frame count, we know the amount of samples that realistically fit into that buffer. 
            var frameBufferSize = frameCount * 9; // and we know the amount of bytes that the buffer will take.
            var adjustedFrameBufferSize = frameBufferSize; //+ (frameBufferSize % 32); // pads buffer to 32 bytes. 
            byte[] adpcm_data = new byte[adjustedFrameBufferSize + 18]; // 9 bytes per 16 samples 

            last = 0;
            penult = 0;
            int absolute = 0;
            int pcmLast = 0;
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
                    if (((ix * 16) + k) >= WaveData.sampleCount)
                        continue; // skip if we're out of samplebuffer, continue to build last frame
                    if ((ix * 16) + k == loopRetSample)
                    {
                        last = lastSample;
                        penult = penultimate;
                    }

                    wavIn[k] = wavFP[(ix * 16) + k];
                }

                // build ADPCM frame
                bananapeel.Pcm16toAdpcm4HLE(wavIn, adpcmOut, ref lastSample, ref penultimate); // convert PCM16 -> ADPCM4
                //banan_brawl.encode_adpcm_16_managed(wavIn, adpcmOut, ref lastSample, ref penultimate, ref pcmLast);
                //banan_brawl.encode_adpcm_16_managed(wavIn, adpcmOut);
                for (int k = 0; k < 9; k++)
                {
                    adpcm_data[adp_f_pos] = adpcmOut[k]; // dump into ADPCM buffer.
                    //Console.WriteLine(adpcmOut[k]);
                    adp_f_pos++; // increment ADPCM byte
                }
            }
            return adpcm_data;
        }


        public static byte[] transform_pcm16_mono_adpcm2_hle(PCM16WAV WaveData, out int adjustedSampleCount, int loopRetSample, out int last, out int penult)
        {
            //adpcm_data = new byte[((WaveData.sampleCount / 9) * 16)];


            int frameCount = (WaveData.sampleCount + 16 - 1) / 16;
            adjustedSampleCount = frameCount * 16; // now that we have a properly calculated frame count, we know the amount of samples that realistically fit into that buffer. 
            var frameBufferSize = frameCount * 5; // and we know the amount of bytes that the buffer will take.
            var adjustedFrameBufferSize = frameBufferSize; //+ (frameBufferSize % 32); // pads buffer to 32 bytes. 
            byte[] adpcm_data = new byte[adjustedFrameBufferSize + 10]; // 9 bytes per 16 samples 

            last = 0;
            penult = 0;
            int absolute = 0;
            int pcmLast = 0;
            //Console.WriteLine($"\n\n\n{WaveData.sampleCount} samples\n{frameCount} frames.\n{frameBufferSize} bytes\n{adjustedFrameBufferSize} padded bytes. ");
            var adp_f_pos = 0; // ADPCM position

            var lastSample = 0;
            var penultimate = 0;

            var wavFP = WaveData.buffer;
            // transform one frame at a time
            for (int ix = 0; ix < frameCount; ix++)
            {
                short[] wavIn = new short[16];
                byte[] adpcmOut = new byte[5];
                for (int k = 0; k < 16; k++)
                {
                    if (((ix * 16) + k) >= WaveData.sampleCount)
                        continue; // skip if we're out of samplebuffer, continue to build last frame
                    if ((ix * 16) + k == loopRetSample)
                    {
                        last = lastSample;
                        penult = penultimate;
                    }

                    wavIn[k] = wavFP[(ix * 16) + k];
                }

                // build ADPCM frame
                bananapeel.Pcm16toAdpcm2(wavIn, adpcmOut, ref lastSample, ref penultimate); // convert PCM16 -> ADPCM4
                //banan_brawl.encode_adpcm_16_managed(wavIn, adpcmOut, ref lastSample, ref penultimate, ref pcmLast);
                //banan_brawl.encode_adpcm_16_managed(wavIn, adpcmOut);
                for (int k = 0; k < 5; k++)
                {
                    adpcm_data[adp_f_pos] = adpcmOut[k]; // dump into ADPCM buffer.
                    //Console.WriteLine(adpcmOut[k]);
                    adp_f_pos++; // increment ADPCM byte
                }
            }
            return adpcm_data;
        }

        public static byte[] transform_pcm16_pcm8(PCM16WAV WaveData, out int adjustedSampleCount, int loopRetSample, out int last, out int penult)
        {
            //adpcm_data = new byte[((WaveData.sampleCount / 9) * 16)];

            last = 0;
            penult = 0;
            
            int frameCount = WaveData.sampleCount;
            // now that we have a properly calculated frame count, we know the amount of samples that realistically fit into that buffer. 
            adjustedSampleCount = frameCount;
            byte[] pcm8Data = new byte[frameCount];

            // transform one frame at a time
            for (int ix = 0; ix < frameCount; ix++)
            {
                pcm8Data[ix] = (byte)((sbyte)(WaveData.buffer[ix] >> 8));
            }
            return pcm8Data;
        }



        private static JWaveDescriptor[] build_aw(string outFile, string projFolder,  minifiedScene scnData, Dictionary<int,JWaveDescriptor> waveTable, string awOutput) 
        {

            var bank_format = cmdarg.findDynamicStringArgument("-encode-format", "adpcm4hle");
         
            var awOutHnd = File.Open($"{awOutput}/{scnData.awfile}", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var awPadding = cmdarg.findDynamicNumberArgument("-awpadding", 32);
            var awOutWt = new BeBinaryWriter(awOutHnd);
            var total_aw_offset = 0;
            var ret = new JWaveDescriptor[scnData.waves.Length];
            for (int wvi = 0; wvi < scnData.waves.Length; wvi++)
            {
                var miniIndex = scnData.waves[wvi];
                var wData = waveTable[miniIndex];
# if DEBUG
  
                    //Console.WriteLine($"{projFolder}/ref/{miniIndex}.adp");
                    //Console.ReadLine();
                
#endif
                int last = wData.last;
                int penult = wData.penult;

                byte[] adpcm_data;

                var cWaveFile = $"{projFolder}/custom/{miniIndex}.wav";
                if (File.Exists(cWaveFile))
                {
                    var strm = File.OpenRead(cWaveFile);
                    var strmInt = new BinaryReader(strm);
                    Console.WriteLine(cWaveFile);
                   // Console.WriteLine(cWaveFile);
                    var WaveData = PCM16WAV.readStream(strmInt);
                    if (WaveData == null)
                        assertHaltStr($"ABORT: '{cWaveFile} has invalid format.");

                    assertHalt(WaveData.sampleRate > 48000, $"ABORT: '{cWaveFile}' has samplerate {WaveData.sampleRate}hz (Max: 32000hz)");
                    assertHalt(WaveData.channels > 1, $"ABORT: '{cWaveFile}' has too many channels {WaveData.channels}chn (Max: 1)");
                    // Console.WriteLine($"\n\t*** Packing custom wave {cWaveFile}");
                    int samplesCount = 0;

                    if (WaveData.sampler.loops != null && WaveData.sampler.loops.Length > 0)
                    {
                        wData.loop = true;
                        wData.loop_start = WaveData.sampler.loops[0].dwStart;
                        wData.loop_end = WaveData.sampler.loops[0].dwEnd;
                    }
                    else
                    {
                        wData.loop = false;
                    }

                    last = 0;
                    penult = 0;

                    var byteInfo = new byte[0];
                  
                    switch (bank_format)
                    {
                        case "pcm8":
                            byteInfo = transform_pcm16_pcm8(WaveData, out samplesCount, wData.loop_start, out last, out penult);
                            wData.format = 2;
                            break;
                        case "apdcm2": // And that Eureka moment hits you like a cop caaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaar!
                            byteInfo = transform_pcm16_mono_adpcm2_hle(WaveData, out samplesCount, wData.loop_start, out last, out penult);
                            wData.format = 1;
                            break;
                        case "adpcm4":
                            byteInfo = transform_pcm16_mono_adpcm(WaveData, out samplesCount, wData.loop_start, out last, out penult);
                            wData.format = 0;
                            break;
                        case "adpcm4hle":
                            byteInfo = transform_pcm16_mono_adpcm4_hle(WaveData, out samplesCount, wData.loop_start, out last, out penult);
                            wData.format = 0;
                            break;                            
                        default:
                            assertHaltStr("Unknown encode format '{0}'", bank_format);
                            break;
                    }
                    
                    wData.last = 0;
                    wData.penult = 0;
           
                    adpcm_data = byteInfo;
                    wData.sampleRate = WaveData.sampleRate;
                    wData.sampleCount = samplesCount; // __wow__
                }
                else
                {
                    var reffile = $"{projFolder}/ref/{miniIndex}.adp";
                    assertHalt(!File.Exists(reffile), "ABORT: Could not find reference file: {0} (Either custom WAV or ADP needed for rebuild, no empties)", reffile);
                    adpcm_data = File.ReadAllBytes(reffile);
                }

                wData.wsys_start = (int)awOutHnd.Position;
                wData.wsys_size = adpcm_data.Length;
                awOutHnd.Write(adpcm_data, 0, adpcm_data.Length);
                //necessary_size -= adpcm_data.Length;

                //for (int i = 0; i < necessary_size; i++)
                   // awOutHnd.WriteByte(0);

                ret[wvi] = wData;
                var deltaPadding = util.padTo(awOutWt, awPadding); // pad to 32 bytes go to hell

                awOutHnd.Flush();
                total_aw_offset = (int)awOutWt.BaseStream.Position;      

                util.consoleProgress($"\t->Rendering {scnData.awfile}...", wvi, scnData.waves.Length - 1, true);
                
            }
            Console.WriteLine();
           
            return ret;
        }

        private static int writeWave(BeBinaryWriter wsysWriter, JWaveDescriptor wData)
        {
            var ret = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write((byte)0xCC);
            wsysWriter.Write((byte)wData.format);
            wsysWriter.Write((byte)wData.key);
            wsysWriter.Write((byte)0);

            wsysWriter.Write((float)wData.sampleRate);
            wsysWriter.Write(wData.wsys_start);
            wsysWriter.Write(wData.wsys_size); // SHOULD spit out exact frame length. holy FUCK.
            wsysWriter.Write(wData.loop ? 0xFFFFFFFF : 0x00000000);
            wsysWriter.Write(wData.loop_start);
            wsysWriter.Write(wData.loop_end);
            wsysWriter.Write(wData.sampleCount);
            wsysWriter.Write((short)wData.last);
            wsysWriter.Write((short)wData.penult);
            wData.mOffset = ret;
            wsysWriter.Write(0);
            wsysWriter.Write(0xCCCCCCCC);
            wsysWriter.Flush();

            return ret;
        }

        private static int writeGroup(BeBinaryWriter wsysWriter, minifiedScene scnData, JWaveDescriptor[] waves)
        {
                var ret = (int)wsysWriter.BaseStream.Position;
                var name = Encoding.ASCII.GetBytes(scnData.awfile);
                wsysWriter.BaseStream.Write(name, 0, name.Length);
                for (int i = 0; i < 0x70 - name.Length; i++)
                    wsysWriter.Write((byte)0);
                wsysWriter.Write(scnData.waves.Length);
        
                for (int i = 0; i < waves.Length; i++)
                {
                    wsysWriter.Write(waves[i].mOffset);
                }
            return ret;
        }

        private static void writeAnchoredReturnInt(BeBinaryWriter wsysWriter, int address, int value)
        {
            var anch = wsysWriter.BaseStream.Position;
            wsysWriter.BaseStream.Position = address;
            wsysWriter.Write(value);
            wsysWriter.BaseStream.Position = anch;
        }

        private static int writeWINF(BeBinaryWriter wsysWriter, int[] winfPointers)
        {
            var ret = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write(0x57494E46);
            wsysWriter.Write(winfPointers.Length);
            for (int i = 0; i < winfPointers.Length; i++)
                wsysWriter.Write(winfPointers[i]);
            return ret;
        }

      
        private static int writeWaveID(BeBinaryWriter wsysWriter, int waveid, int wsysid)
        {
            var ret = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write((short)wsysid);
            wsysWriter.Write((short)waveid);
            for (int i = 0; i < 0xB; i++)
                wsysWriter.Write(0); // empty?
            wsysWriter.Write(0xCCCCCCCC);
            wsysWriter.Write(0xFFFFFFFF); // what the fuck...

            return ret;
        }

        private static int writeScene(BeBinaryWriter wsysWriter,int[] waveIDPointers)
        {
            var cdfPointer = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write(0x432D4446); // C-DF 
            wsysWriter.Write(waveIDPointers.Length); // Length
            for (int i = 0; i < waveIDPointers.Length; i++)
                wsysWriter.Write(waveIDPointers[i]); // empty?
            util.padTo(wsysWriter, 32); // PAD TO 32

            // C-EX and C-ST aren't used, ever. They're not even loaded in the code
            // but just to stay faithful to the structure.
            var cexPointer = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write(0x432D4558); // C-EX
            wsysWriter.Write(0);
            util.padTo(wsysWriter, 32);
            var cstPointer = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write(0x432D5354); // C-ST
            wsysWriter.Write(0);
            util.padTo(wsysWriter, 32);

            var ret = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write(0x53434E45);
            wsysWriter.Write(0L); // 8 bytes 
            wsysWriter.Write(cdfPointer);
            wsysWriter.Write(cexPointer);
            wsysWriter.Write(cstPointer);
            util.padTo(wsysWriter, 32);
            return ret;
        }

        private static int writeWBCT(BeBinaryWriter wsysWriter, int[] scenePointers)
        {
            var ret = (int)wsysWriter.BaseStream.Position;
            wsysWriter.Write(0x57424354);
            wsysWriter.Write(0xFFFFFFFF);
            wsysWriter.Write(scenePointers.Length);
            for (int i = 0; i < scenePointers.Length; i++)
                wsysWriter.Write(scenePointers[i]);
            util.padTo(wsysWriter,32);

            return ret;
        }
        private static int fuck = 0;
        public static unsafe void pack_do(string projFolder, string outfile)
        {
            assertHalt(!Directory.Exists(projFolder), $"Project folder '{projFolder}' could not be found");
            assertHalt(!File.Exists($"{projFolder}/manifest.json"), "Could not find 'manifest.json'") ;
            assertHalt(!File.Exists($"{projFolder}/wavetable.json"), "Could not find 'wavetable.json'");
            int highest_sound_id = 0; // apparently really important.
            string awOutput = null;
            try
            {
                awOutput = createProjectFolder(outfile);
            } catch (Exception E) { cmdarg.assert($"Could not create export folder. ({E.Message})"); }
            Console.WriteLine($"Packing .aw's to {Path.GetFileName(awOutput)}");
            var sceneCount = 0;
            var wsProject = JsonConvert.DeserializeObject<wsysProject>(File.ReadAllText($"{projFolder}/manifest.json"));
            var waveTable = JsonConvert.DeserializeObject<Dictionary<int,JWaveDescriptor>>(File.ReadAllText($"{projFolder}/{wsProject.waveTable}"));
            //var waveIDPtr = JsonConvert.DeserializeObject<Dictionary<int, int>>(File.ReadAllText($"{projFolder}/{wsProject.waveTable}"));
            sceneCount = wsProject.sceneOrder.Length; // Grab Scene Count
            
            var wsysFile = File.OpenWrite($"{projFolder}/out.wsy");
            var wsysWriter = new BeBinaryWriter(wsysFile);

            var sceneProjects = new minifiedScene[wsProject.sceneOrder.Length];

            for (int sci = 0; sci < wsProject.sceneOrder.Length; sci++)
            {
                cmdarg.assert(!File.Exists($"{projFolder}/{wsProject.sceneOrder[sci]}"), $"Cannot find scene file '{projFolder}/{wsProject.sceneOrder[sci]}' from manifest.");
                var scnData = JsonConvert.DeserializeObject<minifiedScene>(File.ReadAllText($"{projFolder}/{wsProject.sceneOrder[sci]}"));
                sceneProjects[sci] = scnData;
            }

            /* write WSYS header */
            wsysWriter.Write(0x57535953); // WSYS;
            wsysWriter.Write(0); // Temporary size;
            wsysWriter.Write(wsProject.id); // Global ID
            wsysWriter.Write(0); //0? 
            wsysWriter.Write(0); // Offset to WINF
            wsysWriter.Write(0); // Offset to WBCT
            wsysWriter.Write(0L); // 8 bytes padding
            util.padTo(wsysWriter, 32);

            int[] scenePointers = new int[sceneProjects.Length];
            int[] groupPointers = new int[sceneProjects.Length];

            for (int sI = 0; sI < sceneProjects.Length; sI++)
            {
                var currentScene = sceneProjects[sI];
                // Build AW
                var waves = build_aw(currentScene.awfile, projFolder, currentScene, waveTable, awOutput);
                var wavePointers = new int[waves.Length];
                for (int wvIndex = 0; wvIndex < waves.Length; wvIndex++)
                    wavePointers[wvIndex] = writeWave(wsysWriter, waves[wvIndex]);

                util.padTo(wsysWriter, 32); // Pad to 32
                // Write waveGroup
                groupPointers[sI] = writeGroup(wsysWriter, currentScene, waves);
                util.padTo(wsysWriter, 32);
            }
            util.padTo(wsysWriter, 32); // Pad to 32
            var winfOffset = writeWINF(wsysWriter, groupPointers);
            writeAnchoredReturnInt(wsysWriter, 0x10, winfOffset); // write to header offset of WINF. 
            util.padTo(wsysWriter, 32); // Pad to 32

            for (int sI = 0; sI < sceneProjects.Length; sI++)
            {
                var currentScene = sceneProjects[sI];
                var waves = currentScene.waves;
                // Build AW
                var waveIDPointers = new int[waves.Length];
                for (int wvIndex = 0; wvIndex < waves.Length; wvIndex++)
                {
                    waveIDPointers[wvIndex] = writeWaveID(wsysWriter, waves[wvIndex], sI);
                    if (waves[wvIndex] > highest_sound_id)
                        highest_sound_id = waves[wvIndex] + 1;
                    //uniqueSounds[waves[wvIndex]] = true;
                }
                util.padTo(wsysWriter, 32); // Pad to 32
                // Write waveGroup
                scenePointers[sI] = writeScene(wsysWriter, waveIDPointers);
            }

            

            var wbctOffset = writeWBCT(wsysWriter, scenePointers);
            writeAnchoredReturnInt(wsysWriter, 0x14, wbctOffset); // write to header offset of WBCT           
            wsysWriter.Flush();
            var finalSize = (int)wsysWriter.BaseStream.Position;
            writeAnchoredReturnInt(wsysWriter, 0x4, finalSize); // write to header offset of WINF. 
            writeAnchoredReturnInt(wsysWriter, 0xC, highest_sound_id); // really fucking weird requirement but ok. 
            wsysWriter.Close();
            File.Delete($"{outfile}");
            File.Move($"{projFolder}/out.wsy", $"{outfile}");
        }
    }
}
