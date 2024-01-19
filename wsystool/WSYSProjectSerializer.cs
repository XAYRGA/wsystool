using bananapeel;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using xayrga.byteglider;

namespace wsystool
{
    internal class WSYSProjectSerializer : WSYSProject
    {
        public string DefaultFormat = "adpcm4";
        private Dictionary<int, WSYSProjectCustomWave> customWaveInfo = new();
        List<WSYSProjectSceneContainer> projectScenes;
        WaveSystem waveSystem;


        private PCM16WAV loadWavFile(string file)
        {
            Console.WriteLine($"[wsystool] Loading custom wave {file}");
            PCM16WAV wave = null;
            using (Stream fStrm = File.OpenRead(file))
            using (BinaryReader bread = new BinaryReader(fStrm))
            {
                wave = PCM16WAV.readStream(bread);
            }

            if (wave.channels != 1)
                throw new WSYSProjectException($"{file} has over 1 channel. All waves must be MONO PCM16");
            else if (wave.bitsPerSample != 16)
                throw new WSYSProjectException($"{file} bitsPersSample!=16. All waves must be MONO PCM16");

            return wave;
        }

        private string extractNumericPrefix(string name)
        {
            string outValue = "";
            byte index = 0;
            while (index < name.Length && name[index] <= '9' && name[index] >= '1')
                outValue += name[index++];
            return outValue;
        }


        private void loadInlineCustomWaves(string folder)
        {
            var files = Directory.GetFiles(folder, "*.wav");
            foreach (var file in files)
            {
                var justFileName = extractNumericPrefix(Path.GetFileNameWithoutExtension(file));
                var waveID = 0;

                if (!Int32.TryParse(justFileName, out waveID))
                    continue;

                if (!customWaveInfo.ContainsKey(waveID)) // If we already have a way we've specified to handle this, let's not import over it.
                    customWaveInfo.Add(waveID, new WSYSProjectCustomWave()
                    {
                        Format = "adpcm4", // Adpcm4 is default for gamecube
                        Key = 60, // 60 is middle C
                        FileName = Path.GetFileName(file)
                    });
            }
        }

        private void loadWaveBuffers(string folder)
        {
            Dictionary<int, WSYSWave> newWaveTable = new Dictionary<int, WSYSWave>();
            foreach (KeyValuePair<int, WSYSWave> waveInstance in WaveTable)
            {
                var k = waveInstance.Key;
                var currentWave = waveInstance.Value;

                var customWaveFile = $"{folder}/custom/{k}.wav";
                var standardBufferFile = $"{folder}/reference/{k}.abf";

                var hasWaveInfo = customWaveInfo.ContainsKey(k);

                if (hasWaveInfo)
                {
                    var wInfo = customWaveInfo[k];
                    if (wInfo.FileName != null)
                        customWaveFile = $"{folder}/custom/{wInfo.FileName}";
                }

                var customFileExists = File.Exists(customWaveFile);
                if (!customFileExists && hasWaveInfo)
                    throw new WSYSProjectException($"Error for waveID {k}! If the wavetable_custom.json contains an entry for {k}, the accompanying WAV file must exist in the 'custom' folder! ({customWaveFile})");
                else if (customFileExists)
                {
                    var wavFile = loadWavFile(customWaveFile);
                    currentWave.sampleCount = wavFile.sampleCount;
                    currentWave.sampleRate = wavFile.sampleRate;

                    if (wavFile.sampler.loops != null && wavFile.sampler.loops.Length > 0)
                    {
                        currentWave.loop = true;
                        currentWave.loop_start = wavFile.sampler.loops[0].dwStart;
                        currentWave.loop_end = wavFile.sampler.loops[0].dwEnd;
                    }

                    switch (currentWave.format)
                    {
                        case 0: // GCAFCADPCM4
                            if (currentWave.loop)
                                WaveBuffers[k] = bananapeel.mux.PCM16TOADPCM4(wavFile.buffer, currentWave.loop_start, out currentWave.last, out currentWave.penult);
                            else
                                WaveBuffers[k] = bananapeel.mux.PCM16TOADPCM4(wavFile.buffer);
                            break;
                        case 1: // GCAFCADPCM2
                            if (currentWave.loop)
                                WaveBuffers[k] = bananapeel.mux.PCM16TOADPCM2(wavFile.buffer, currentWave.loop_start, out currentWave.last, out currentWave.penult);
                            else
                                WaveBuffers[k] = bananapeel.mux.PCM16TOADPCM2(wavFile.buffer);
                            break;
                        case 2: // PCM8
                            WaveBuffers[k] = bananapeel.mux.PCM1628(wavFile.buffer);
                            break;
                        case 3: // PCM16                    
                            WaveBuffers[k] = bananapeel.mux.PCM16ShortToByte(bananapeel.mux.PCM16BYTESWAP(wavFile.buffer));
                            break;
                        default:
                            throw new WSYSProjectException($"Unsupported encode format {currentWave.format}");
                    }
#if DEBUG
                    if (currentWave.loop)
                        Console.WriteLine($"ENCODER: CustomWave {k} fmt={formatNumberToString(currentWave.format)}, bfr=0x{WaveBuffers[k].Length:X}, loop: ye ({currentWave.loop_start}, {currentWave.loop_end} L={currentWave.last}, P={currentWave.penult})");
                    else
                        Console.WriteLine($"ENCODER: CustomWave {k} fmt={formatNumberToString(currentWave.format)}, bfr=0x{WaveBuffers[k].Length:X}, loop: no");
#endif
                }
                else if (File.Exists(standardBufferFile))
                {
                    // Using the standard buffer, don't post process
                    var data = File.ReadAllBytes(standardBufferFile);
                    WaveBuffers[k] = data;
                }
                else
                    throw new WSYSProjectException($"Cannot locate buffer file for waveid {k} ({standardBufferFile})");

                newWaveTable[k] = currentWave;
            }
            WaveTable = newWaveTable;
        }

        private void integrateCustomWaves()
        {
            foreach (KeyValuePair<int, WSYSProjectCustomWave> kvpWaveInfo in customWaveInfo)
            {
                var key = kvpWaveInfo.Key;
                var value = kvpWaveInfo.Value;

                var newFormat = formatStringToNumber(value.Format);
                if (newFormat < 0)
                    throw new WSYSProjectException($"Custom Wave ID {key} has invalid format {value.Format}");

                WaveTable[key] = new WSYSWave() { format = (byte)newFormat, key = value.Key, };
            }
        }

        private static string formatNumberToString(int format)
        {
            switch (format)
            {
                case 0:
                    return "adpcm4";
                case 1:
                    return "adpcm2";
                case 2:
                    return "pcm8";
                case 3:
                    return "pcm16";
                default: return "UNSUPPORTED";
            }
        }

        private static int formatStringToNumber(string format)
        {
            switch (format)
            {
                case "adpcm4":
                    return 0;
                case "adpcm2":
                    return 1;
                case "pcm8":
                    return 2;
                case "pcm16":
                    return 3;
                default: return -1;
            }
        }


        public WaveSystem LoadProjectData(string folder)
        {
            if (!Directory.Exists(folder))
                throw new WSYSProjectException($"WSYS Project doesn't exist {folder}");

            if (!File.Exists($"{folder}/wsys.json"))
                throw new WSYSProjectException($"Cannot load WSYS manifest file {folder}");

            if (!File.Exists($"{folder}/wavetable.json"))
                throw new WSYSProjectException($"Cannot load WSYS wavetable file {folder}");

            var tempData = File.ReadAllText($"{folder}/wsys.json");
            var Project = JsonConvert.DeserializeObject<WSYSProjectContainer>(tempData);

            tempData = File.ReadAllText($"{folder}/wavetable.json");
            WaveTable = JsonConvert.DeserializeObject<Dictionary<int, WSYSWave>>(tempData);

            if (File.Exists($"{folder}/wavetable_custom.json"))
            {
                tempData = File.ReadAllText($"{folder}/wavetable_custom.json");
                customWaveInfo = JsonConvert.DeserializeObject<Dictionary<int, WSYSProjectCustomWave>>(tempData);
            }

            // Loads the custom folder into the wavetable.
            loadInlineCustomWaves($"{folder}/custom/");

            // Integrate them into the wavetable.
            integrateCustomWaves();

            loadWaveBuffers(folder);

            projectScenes = new List<WSYSProjectSceneContainer>();
            for (int i = 0; i < Project.Scenes.Count; i++)
                if (!File.Exists($"{folder}/{Project.Scenes[i]}"))
                    throw new WSYSProjectException($"Cannot find scene manifest {Project.Scenes[i]}");
                else
                    projectScenes.Add(JsonConvert.DeserializeObject<WSYSProjectSceneContainer>(File.ReadAllText($"{folder}/{Project.Scenes[i]}")));

            waveSystem = new WaveSystem();
            waveSystem.id = Project.id;

            return waveSystem;
        }

        public void WriteWaveSystem(string wsysFile, string awFolder)
        {

            if (!Directory.Exists(awFolder))
                Directory.CreateDirectory(awFolder);

            waveSystem.Groups = new WSYSGroup[projectScenes.Count];
            waveSystem.Scenes = new WSYSScene[projectScenes.Count];
            for (short i = 0; i < projectScenes.Count; i++)
            {
                var PROJSCENE = projectScenes[i];
                var GRP = new WSYSGroup();
                var SCNE = new WSYSScene();

                GRP.awPath = PROJSCENE.awName;
                SCNE.DEFAULT = new WSYSWaveID[PROJSCENE.waves.Count];
                SCNE.EXTENDED = new WSYSWaveID[0];
                SCNE.STATIC = new WSYSWaveID[0];

                var strm = File.Open($"{awFolder}/{PROJSCENE.awName}", FileMode.Create);
                var wrt = new bgWriter(strm);

                GRP.waves = new WSYSWave[PROJSCENE.waves.Count];
                Console.WriteLine($"[wsystool - {PROJSCENE.awName}] Rendering {PROJSCENE.waves.Count} waves");
                for (int wave = 0; wave < PROJSCENE.waves.Count; wave++)
                {
                    var cWID = (short)PROJSCENE.waves[wave];

                    if (!WaveTable.ContainsKey(cWID))
                        throw new WSYSProjectException($"Waveid {cWID} not found in wavetable but requested by {PROJSCENE.awName}");

                    var cWave = WaveTable[cWID];
                    var buffer = WaveBuffers[cWID];
                    cWave.awOffset = (int)wrt.BaseStream.Position;
                    cWave.awLength = buffer.Length;
                    wrt.Write(buffer);
                    wrt.PadAlign(0x20);

                    SCNE.DEFAULT[wave] = new WSYSWaveID() { GroupID = i, WaveID = cWID };
                    GRP.waves[wave] = cWave;
                }
                waveSystem.Groups[i] = GRP;
                waveSystem.Scenes[i] = SCNE;
                wrt.FlushAndClose();
            }
            var wsyf = File.OpenWrite(wsysFile);
            var wsysWr = new bgWriter(wsyf);
            waveSystem.WriteToStream(wsysWr);
        }


    }
}
