using bananapeel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wsystool
{
    internal class WSYSProjectDeserializer : WSYSProject
    {
        public void LoadWSYS(WaveSystem waveSystem, string AWPath)
        {
            // Rebuild the original wavetable from what's left over in the scenes and groups. 
            for (int sceneGroup = 0; sceneGroup < waveSystem.Groups.Length; sceneGroup++)
            {
                // This is not the way to parse this, but in practice it doesn't show up differently.               
                var cGrp = waveSystem.Groups[sceneGroup];
                var cScn = waveSystem.Scenes[sceneGroup];
                // guess we don't need to worry about it.          

                var awHnd = File.OpenRead($"{AWPath}\\{cGrp.awPath}");

                for (int wave = 0; wave < cGrp.waves.Length; wave++)
                {
                    var gWave = cGrp.waves[wave];
                    var sWave = cScn.DEFAULT[wave];

                    // Don't reload the wave if we already have it
                    if (WaveTable.ContainsKey(sWave.WaveID))
                        continue;

                    WaveTable[sWave.WaveID] = gWave;

                    awHnd.Position = gWave.awOffset;
                    var buffer = new byte[gWave.awLength];
                    WaveBuffers[sWave.WaveID] = buffer;
                    awHnd.Read(buffer, 0, buffer.Length);
                }
                awHnd.Close();
            }
        }


        public void SaveProjectData(WaveSystem waveSystem, string folder)
        {         

            if (!Directory.Exists($"{folder}/reference"))
                Directory.CreateDirectory($"{folder}/reference");

            if (!Directory.Exists($"{folder}/custom"))
                Directory.CreateDirectory($"{folder}/custom");

            if (!Directory.Exists($"{folder}/scenes"))
                Directory.CreateDirectory($"{folder}/scenes");

            SaveBuffers($"{folder}/reference/");

            // Save the wavetable
            File.WriteAllText($"{folder}/wavetable.json", JsonConvert.SerializeObject(WaveTable, Formatting.Indented));

            var prj = new WSYSProjectContainer();
            prj.id = waveSystem.id;
            List<string> Scenes = prj.Scenes;
            for (int i = 0; i < waveSystem.Scenes.Length; i++)
            {
                var scn = waveSystem.Scenes[i];
                var grp = waveSystem.Groups[i];

                var fileName = $"scenes/{Path.GetFileNameWithoutExtension(grp.awPath)}.json";
                var sceneContainer = new WSYSProjectSceneContainer();
                sceneContainer.awName = grp.awPath;

                for (int j = 0; j < grp.waves.Length; j++)
                    sceneContainer.waves.Add(scn.DEFAULT[j].WaveID);

                File.WriteAllText($"{folder}/{fileName}", JsonConvert.SerializeObject(sceneContainer, Formatting.Indented));
                Scenes.Add(fileName);
            }

            File.WriteAllText($"{folder}/wsys.json", JsonConvert.SerializeObject(prj, Formatting.Indented));
        }

        public void SaveBuffers(string folder)
        {
            foreach (var wave in WaveBuffers)
                File.WriteAllBytes($"{folder}/{wave.Key}.abf", wave.Value);
        }

        public void RenderWaveData(string folder)
        {
            foreach (var wave in WaveTable)
            {
                var wData = wave.Value;
                var buffer = WaveBuffers[wave.Key];

                var samples = new short[0];
                switch (wData.format)
                {
                    case 0: // ADPCM4
                        samples = bananapeel.mux.ADPCM4TOPCM16(buffer);
                        break;
                    case 1: // ADPCM2
                        samples = bananapeel.mux.ADPCM2TOPCM16(buffer);
                        break;
                    case 2: // PCM8
                        samples = bananapeel.mux.PCM8216(buffer);
                        break;
                    case 3: // PCM16
                        samples = bananapeel.mux.PCM16ByteToShort(buffer);
                        break;
                    default:
                        throw new InvalidDataException($"bad WSYS format {wData.format}");
                }

                // compose wave file
                var nwf = new PCM16WAV()
                {
                    format = 1,
                    sampleRate = (int)wData.sampleRate,
                    channels = 1,
                    blockAlign = 2,
                    bitsPerSample = 16,
                    buffer = samples,
                };

                // Write the loop point if we have it. 
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
                // Write file
                var fileData = File.OpenWrite($"{folder}/{wave.Key}.wav");
                var fileWriter = new BinaryWriter(fileData);
                nwf.writeStreamLazy(fileWriter);
                fileWriter.Flush();
                fileData.Close();
            }
        }

    }
}
