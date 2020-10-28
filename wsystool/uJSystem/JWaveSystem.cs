using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio
{
    public class JWaveDescriptor
    {
        public ushort format;
        public ushort key;
        public double sampleRate;
        public int sampleCount;

        public int wsys_start;
        public int wsys_size;

        public bool loop;
        public int loop_start;
        public int loop_end;

        public byte[] pcmData;

        public int mOffset;
    }

    public class JC_DFEntry // Helper class for  returning all data when reading each entry inside of the C_DF.
    {
        public short awid;
        public short waveid;
        public int mOffset;
    }

    public class JWaveScene
    {
        public JC_DFEntry[] CDFData;
        public int mOffset;
    }

    public class JWaveGroup
    {
        public string awFile;
        public JWaveDescriptor[] Waves;
   
        public int mOffset;
    }
    public class JWaveSystem
    {
        public int id;
        public JWaveGroup[] Groups;
        public JWaveScene[] Scenes;
        public int mOffset;
    }
}
