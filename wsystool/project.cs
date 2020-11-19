using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wsysbuilder
{
    internal class wsysProject
    {
        public int id;
        public string waveTable;
        public string[] sceneOrder;
    }
    internal class minifiedScene
    {
        public string awfile;
        public int[] waves;
    }
    internal class minifiedWave
    {
        public byte format;
        public int key;
        public bool loop;
        public int loopStart;
        public int loopEnd;
    }
}
