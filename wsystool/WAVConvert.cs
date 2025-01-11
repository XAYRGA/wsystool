using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bananapeel;

namespace wsystool
{
    internal class WAVConvert
    {
        string toFormat;
        string outputFile;
        PCM16WAV inputWav;

        public WAVConvert(PCM16WAV input, string format, string destinationFile) { 
            toFormat = format;   
            inputWav = input; 
        }

        public byte[] Convert()
        {

            switch (toFormat)
            {
                case "adpcm4":
                    return bananapeel.mux.PCM16TOADPCM4(inputWav.buffer);
                case "adpcm2":
                    return bananapeel.mux.PCM16TOADPCM2(inputWav.buffer);
                case "pcm8":
                    return bananapeel.mux.PCM1628(inputWav.buffer);
                default:
                    throw new Exception("Invalid format.");
            }
        }
    }
}
