using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace wsysbuilder
{
    public static class util
    {
        public static void consoleProgress(string txt, int progress, int max, bool show_progress = false)
        {
            var flt_total = (float)progress / max;
            Console.CursorLeft = 0;
            //Console.WriteLine(flt_total);
            Console.Write($"{txt} [");
            for (float i = 0; i < 32; i++)
                if (flt_total > (i / 32f))
                    Console.Write("#");
                else
                    Console.Write(" ");
            Console.Write("]");
            if (show_progress)
                Console.Write($" ({progress}/{max})");           
        }
        public static int padTo(BeBinaryWriter bw, int padding)
        {
            var delta = (int)(bw.BaseStream.Position % padding);
            if (delta == padding)
                return 0;
            for (int i = 0; i < ( padding - delta ); i++)
                bw.Write((byte)0x00);
            return (padding - delta);
        }

        public static int padToInt(int Addr, int padding)
        {
            var delta = (int)(Addr % padding);
            return (padding - delta);        
        }

    }
}
