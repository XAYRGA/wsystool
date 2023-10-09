using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using xayrga.byteglider;
using Newtonsoft.Json;

namespace JaiMaker
{
    internal class WaveSystem
    {
        private const int WSYS = 0x57535953;
        private const int WINF = 0x57494E46;
        private const int WBCT = 0x57424354;
        public int id;
        public int total_sounds;

        public WSYSScene[] Scenes;
        public WSYSGroup[] Groups;

        internal int mBaseAddress = 0;

        private void loadWinf(bgReader rd)
        {
            if (rd.ReadInt32BE() != WINF)
                throw new Exception("WINF corrupt");
            var count = rd.ReadInt32BE();
            var ptrs = rd.ReadInt32ArrayBE(count);
            Groups = new WSYSGroup[count];
            for (int i = 0; i < count; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                Groups[i] = WSYSGroup.CreateFromStream(rd);
            }
        }

        private void loadWbct(bgReader rd)
        {
            if (rd.ReadInt32BE() != WBCT)
                throw new Exception("WBCT corrupt");
            rd.ReadInt32BE(); // Empty?
            var count = rd.ReadInt32BE();
            var ptrs = rd.ReadInt32ArrayBE(count);
            Scenes = new WSYSScene[count];
            for (int i = 0; i < count; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                Scenes[i] = WSYSScene.CreateFromStream(rd);
            }
        }

        public static WaveSystem CreateFromStream(bgReader rd)
        {
            var b = new WaveSystem();
            b.loadFromStream(rd);
            return b;
        }

        private void loadFromStream(bgReader rd)
        {
            if (rd.ReadInt32BE() != WSYS)
                throw new InvalidDataException("Couldn't match WSYS header!");
            var size = rd.ReadInt32BE();
            id = rd.ReadInt32BE();
            total_sounds = rd.ReadInt32BE();

            var winfOffset = rd.ReadInt32BE();
            var wbctOffset = rd.ReadInt32BE();

            rd.BaseStream.Position = winfOffset;
            loadWinf(rd);

            rd.BaseStream.Position = wbctOffset;
            loadWbct(rd);
        }


        private int writeWinf(bgWriter wr)
        {
            for (int i = 0; i < Groups.Length; i++)
                Groups[i].WriteToStream(wr);


            wr.PadAlign(0x20);
            var winfOffs = (int)wr.BaseStream.Position;
            wr.WriteBE(WINF);
            wr.WriteBE(Groups.Length);

            for (int i = 0; i < Groups.Length; i++)
                wr.WriteBE(Groups[i].mBaseAddress);

            return winfOffs;
        }


        private int writeWbct(bgWriter wr)
        {
            for (int i = 0; i < Scenes.Length; i++)
                Scenes[i].WriteToStream(wr);

            wr.PadAlign(0x20);
            var wbctOffs = (int)wr.BaseStream.Position;
            wr.WriteBE(WBCT);
            wr.WriteBE(0);
            wr.WriteBE(Scenes.Length);

            for (int i = 0; i < Scenes.Length; i++)
                wr.WriteBE(Scenes[i].mBaseAddress);

            return wbctOffs;
        }

        public void WriteToStream(bgWriter wr)
        {
            wr.WriteBE(WSYS);
            var ret = wr.BaseStream.Position;
            wr.WriteBE(0); // size
            wr.WriteBE(0); // id
            wr.WriteBE(0); // total 
            wr.WriteBE(0); // wbct
            wr.WriteBE(0); // winf

            wr.PadAlign(0x20);

            var winfOffs = writeWinf(wr);
            var wbctOffs = writeWbct(wr);


            var size = (int)wr.BaseStream.Position;

            // Calculate highest waveid.
            var highest = 0;
            for (int i = 0; i < Scenes.Length; i++)
                for (int b = 0; b < Scenes[i].DEFAULT.Length; b++)
                    if (Scenes[i].DEFAULT[b].WaveID > highest)
                        highest = Scenes[i].DEFAULT[b].WaveID;

            wr.BaseStream.Position = ret;
            wr.WriteBE(size);
            wr.WriteBE(id);
            wr.WriteBE(highest);
            wr.WriteBE(winfOffs);
            wr.WriteBE(wbctOffs);

            wr.BaseStream.Position = size;

            wr.PadAlign(0x20);
        }
    }

    public class WSYSScene 
    {

        private const int SCNE = 0x53434E45;

        private const int C_DF = 0x432D4446;
        private const int C_EX = 0x432D4558;
        private const int C_ST = 0x432D5354;

        public WSYSWaveID[] DEFAULT;
        public WSYSWaveID[] EXTENDED;
        public WSYSWaveID[] STATIC;

        internal int mBaseAddress = 0;

        private WSYSWaveID[] loadContainer(bgReader rd, int type)
        {
            var inType = rd.ReadInt32BE();
            if (inType != type)
                throw new Exception($"Unexpected section type {type:X} != {inType:X}");
            var count = rd.ReadInt32BE();
            var waves = new WSYSWaveID[count];
            var offsets = rd.ReadInt32ArrayBE(count);
            for (int i = 0; i < count; i++)
            {
                rd.BaseStream.Position = offsets[i];
                waves[i] = WSYSWaveID.CreateFromStream(rd);
            }
            return waves;
        }

        private int writeContainer(bgWriter wr, int type, WSYSWaveID[] outw)
        {

            for (int i = 0; i < outw.Length; i++)
                outw[i].WriteToStream(wr);
            wr.PadAlign(32);
            var pos = (int)wr.BaseStream.Position;
            wr.WriteBE(type);
            wr.WriteBE(outw.Length);
            for (int i = 0; i < outw.Length; i++)
                wr.WriteBE(outw[i].mBaseAddress);

            return pos;
        }

        public static WSYSScene CreateFromStream(bgReader rd)
        {
            var b = new WSYSScene();
            b.loadFromStream(rd);
            return b;
        }


        private void loadFromStream(bgReader rd)
        {
            if (rd.ReadInt32BE() != SCNE)
                throw new Exception("SCNE corrupt");
            rd.ReadUInt64(); // Padding? Something???? Always zero.
            var cdfOffset = rd.ReadInt32BE();
            var cexOffset = rd.ReadInt32BE();
            var cstOffset = rd.ReadInt32BE();

            rd.BaseStream.Position = cdfOffset;
            DEFAULT = loadContainer(rd, C_DF);
            rd.BaseStream.Position = cexOffset;
            EXTENDED = loadContainer(rd, C_EX);
            rd.BaseStream.Position = cstOffset;
            STATIC = loadContainer(rd, C_ST);
        }

        public void WriteToStream(bgWriter wr)
        {
            var cdfOffset = writeContainer(wr, C_DF, DEFAULT);
            var cexOffset = writeContainer(wr, C_EX, EXTENDED);
            var cstOffset = writeContainer(wr, C_ST, STATIC);

            wr.PadAlign(0x20);
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.WriteBE(SCNE);
            wr.WriteBE(0L);
            wr.WriteBE(cdfOffset);
            wr.WriteBE(cexOffset);
            wr.WriteBE(cstOffset);
        }
    }

    public class WSYSGroup
    {
        public string awPath;
        public WSYSWave[] waves;

        internal int mBaseAddress = 0;

        public static WSYSGroup CreateFromStream(bgReader rd)
        {
            var b = new WSYSGroup();
            b.loadFromStream(rd);
            return b;
        }

        private void loadFromStream(bgReader rd)
        {
            awPath = "";
            var stringBuff = rd.ReadBytes(0x70);
            for (int i = 0; i < 0x70; i++)
                if (stringBuff[i] != 0)
                    awPath += (char)stringBuff[i];
                else
                    break;

            var count = rd.ReadInt32BE();
            var ptrs = rd.ReadInt32ArrayBE(count);
            waves = new WSYSWave[ptrs.Length];
            for (int i = 0; i < ptrs.Length; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                waves[i] = WSYSWave.CreateFromStream(rd);
            }
        }

        public void WriteToStream(bgWriter wr)
        {
            // We write the waves first so their offsets are allocated
            for (int i = 0; i < waves.Length; i++)
                waves[i].WriteToStream(wr);

            wr.PadAlign(0x20);

            mBaseAddress = (int)wr.BaseStream.Position;
            byte[] buff = new byte[0x70];
            for (int i = 0; i < awPath.Length; i++)
                buff[i] = (byte)awPath[i];
            wr.Write(buff);
            wr.WriteBE(waves.Length);

            for (int i = 0; i < waves.Length; i++)
                wr.WriteBE(waves[i].mBaseAddress);
        }
    }


    public class WSYSWaveID 
    {
        public short GroupID;
        public short WaveID;

        internal int mBaseAddress = 0;

        public void loadFromStream(bgReader rd)
        {
            GroupID = rd.ReadInt16BE();
            WaveID = rd.ReadInt16BE();
            rd.ReadInt32BE(); // CCCCCCCC
            rd.ReadInt32BE(); // FFFFFFFF
        }
        public static WSYSWaveID CreateFromStream(bgReader rd)
        {
            var b = new WSYSWaveID();
            b.loadFromStream(rd);
            return b;
        }

        public  void WriteToStream(bgWriter wr)
        {
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.WriteBE(GroupID);
            wr.WriteBE(WaveID);
            wr.Write(new byte[0x2C]); // Empty?
            wr.WriteBE(0xCCCCCCCC); // Uninitialized stack
            wr.WriteBE(0xFFFFFFFF); // nice
        }
    }


    public class WSYSWave
    {
        public byte format;
        public byte key;
        public float sampleRate;
        public int sampleCount;

        [JsonIgnore]
        public int awOffset;
        [JsonIgnore]
        public int awLength;

        public bool loop;
        public int loop_start;
        public int loop_end;

        public short last;
        public short penult;


        internal int mBaseAddress = 0;

        public void loadFromStream(bgReader rd)
        {
            rd.ReadByte(); // Empty.
            format = rd.ReadByte();
            key = rd.ReadByte();
            rd.ReadByte(); // empty. 
            sampleRate = rd.ReadSingleBE();
            awOffset = rd.ReadInt32BE();
            awLength = rd.ReadInt32BE();
            loop = rd.ReadUInt32() == 0xFFFFFFFF;
            loop_start = rd.ReadInt32BE();
            loop_end = rd.ReadInt32BE();
            sampleCount = rd.ReadInt32BE();
            last = rd.ReadInt16BE();
            penult = rd.ReadInt16BE();

            rd.ReadInt32BE(); // Zero.
            rd.ReadInt32BE(); // 0xCCCCCCCC Uninitialized stack
        }

        public static WSYSWave CreateFromStream(bgReader rd)
        {
            var b = new WSYSWave();
            b.loadFromStream(rd);
            return b;
        }

        public void WriteToStream(bgWriter wr)
        {
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.WriteBE((byte)0xCC);
            wr.WriteBE(format);
            wr.WriteBE(key);
            wr.WriteBE((byte)0);
            wr.WriteBE(sampleRate);
            wr.WriteBE(awOffset);
            wr.WriteBE(awLength);
            wr.WriteBE(loop ? 0xFFFFFFFF : 0);
            wr.WriteBE(loop_start);
            wr.WriteBE(loop_end);
            wr.WriteBE(sampleCount);
            wr.WriteBE(last);
            wr.WriteBE(penult);
            wr.WriteBE(0);
            wr.WriteBE(0xCCCCCCCC);
        }
    }
}
