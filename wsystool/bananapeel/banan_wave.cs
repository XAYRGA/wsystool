using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace wsystool
{

    public class WAVCuePoint
    {
        public int id;
        public int order;
        public int chunkid;
        public int chunkStart;
        public int blockStart;
        public int frameOffset;         
        
        public static WAVCuePoint readStream(BinaryReader br)
        {
            return new WAVCuePoint()
            {
                id = br.ReadInt32(),
                order = br.ReadInt32(),
                chunkid = br.ReadInt32(),
                chunkStart = br.ReadInt32(),
                blockStart = br.ReadInt32(),
                frameOffset = br.ReadInt32()
            };
        }

        public void writeStream(BinaryWriter bw)
        {
            bw.Write(id);
            bw.Write(order);
            bw.Write(chunkid);
            bw.Write(chunkStart);
            bw.Write(blockStart);
            bw.Write(frameOffset);
        }
    }

    public class PCM16WAV
    {


        private static byte[] wavhead = new byte[44] {
                        0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,  0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
                        0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,  0x00, 0x00, 0x00, 0x00
        };

        public const int FORMAT = 0x20746D66;
        public const int DATA = 0x61746164;
        public const int CUE = 0x20657563;
        public const int SMPL = 0x6C706D73; // OpenMPT sample chunk.

        public int chunkSize;
        public short format;
        public short channels;
        public int sampleRate;
        public int byteRate;
        public short blockAlign;
        public short bitsPerSample;
        public int sampleCount; 
        public short[] buffer;
        public WAVCuePoint[] cuePoints;

        private static int findChunk(BinaryReader br, int chunkid)
        {
            int nextCID = 0;
            while (true) {
                nextCID = br.ReadInt32();
                if (chunkid != nextCID)
                {
                    var nextSZ = br.ReadInt32();
                    if (br.BaseStream.Position + nextSZ >= br.BaseStream.Length)
                        return -1;
                    br.BaseStream.Position += nextSZ;
                }
                else
                    return (int)br.BaseStream.Position; // return pos minus read int
            }
        }

        public static PCM16WAV readStream(BinaryReader br)
        {
            // Check for RIFF magic
            if (br.ReadUInt32() != 0x46464952u)
                return null;
            int riff_chunck_size = br.ReadInt32();
            // Check for WAVE format
            if (br.ReadUInt32() != 0x45564157u)
                return null;
            // Find + load format chunk
            var wavHAnch = br.BaseStream.Position;
            var fmtOfs = findChunk(br,FORMAT); // Locate format chunk
            if (fmtOfs < 0)
                return null; // format chunk wasn't found. Abort.
            br.BaseStream.Position = fmtOfs;
            // Format chunk (see variable names)
            var NewWave = new PCM16WAV()
            {
                chunkSize = br.ReadInt32(),
                format = br.ReadInt16(),
                channels = br.ReadInt16(),
                sampleRate = br.ReadInt32(),
                byteRate = br.ReadInt32(),
                blockAlign = br.ReadInt16(),
                bitsPerSample = br.ReadInt16()
            };
            // Find + load PCM16 buffer chunk
            br.BaseStream.Position = wavHAnch; // Seek back to section anchor (first section magic)
            var datOfs = findChunk(br, DATA); // locate data chunk
            if (datOfs < 0) // no PCM buffer chunk, abort. 
                return null;
            var datSize = br.ReadInt32(); // section size. 
            NewWave.sampleCount = datSize / NewWave.blockAlign; // calculate sample count (data length / block alignment)
            NewWave.buffer = new short[NewWave.sampleCount]; // initialize PCM buffer array
            for (int i=0; i < NewWave.sampleCount; i++)
                NewWave.buffer[i] = br.ReadInt16(); // sprawl out samples into array

            br.BaseStream.Position = wavHAnch; // Seek back to section anchor (first section magic)
            // Load cue points (optional)
            var cueOfs = findChunk(br, CUE); // Locate "cue " chunk
            if (cueOfs > 0) {
                var cueLength = br.ReadInt32(); // Reading just to align. (Section size)
                var cueCount = br.ReadInt32();
                NewWave.cuePoints = new WAVCuePoint[cueCount]; // initialize array with length
                for (int i = 0; i < cueCount; i++)
                    NewWave.cuePoints[i] = WAVCuePoint.readStream(br); // read individual points into array
            }
            br.BaseStream.Position = wavHAnch;
            var smplOfs = findChunk(br, SMPL); // find OpenMPT sample chunk.
            if (smplOfs > 0)
            {
                var smplSize = br.ReadInt32();
                br.ReadBytes(0x2C); /// ooooh fuuuuuu
            

            }
            
       
            return NewWave;
        }

        public void writeStreamLazy(BinaryWriter bw)
        {
            var bufferLength = buffer.Length * 2; // Since the buffer is shorts, the length will be double. 
            // Assemble header
            bw.BaseStream.Write(wavhead, 0, wavhead.Length);
            bw.BaseStream.Position = 24;
            bw.Write((int)sampleRate); // Sample rate.... twice?
            bw.Write((int)sampleRate);
            bw.BaseStream.Position = 40;
            // Also includes WAVEfmt                
            bw.Write((int)bufferLength);
            for (int i = 0; i < buffer.Length; i++) 
                bw.Write(buffer[i]); // sprawl out each short
          
            if (cuePoints != null && cuePoints.Length > 0)
            {
                bw.Write(CUE);
                bw.Write((cuePoints.Length * 24) + 4);
                bw.Write(cuePoints.Length);
                for (int i = 0; i < cuePoints.Length; i++)
                {
                    cuePoints[i].writeStream(bw);
                }
            }
            var tl_end = (int)bw.BaseStream.Position;
            bw.BaseStream.Position = 4;
            bw.Write(tl_end - 8);
            bw.Flush();
        }
    }
}
