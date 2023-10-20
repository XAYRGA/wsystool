using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using xayrga.byteglider;
using bananapeel;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wsystool
{
   class WSYSProjectContainer
    {
        public int id;
        public List<string> Scenes = new List<string>();
    }
    class WSYSProjectSceneContainer
    {
        public string awName;
        public List<int> waves = new List<int>();
    }

    class WSYSProjectCustomWave
    {
        public byte Key = 64;
        public string Format = "adpcm4";
        public string FileName = null;
    }

    internal class WSYSProject 
    {
        public Dictionary<int, WSYSWave> WaveTable = new Dictionary<int, WSYSWave>();
        public Dictionary<int, byte[]> WaveBuffers = new Dictionary<int, byte[]>();
    }
    internal class WSYSProjectException : Exception
    {
        public WSYSProjectException(string msg) : base(msg) { }
    }
}
