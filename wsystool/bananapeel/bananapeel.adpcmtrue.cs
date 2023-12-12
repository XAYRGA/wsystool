using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



// Decoders by Arookas!
// Source is from https://github.com/arookas/flaaffy/blob/master/mareep/waveform.cs 

/* 
MIT License

Copyright (c) Arookas 2017 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


// Encoders by Zyphro!
/* 
MIT License

Copyright (c) Zyphro 2023

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



namespace bananapeel
{
    public static partial class ADPCMTRUE
    {
        static int[] sSigned2BitTable = new int[4] {
            0, 1, -2, -1,
        };
        static int[] sSigned4BitTable = new int[16] {
            0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1,
        };
        static short[,] sAdpcmCoefficents = new short[16, 2] {
            { 0, 0, }, { 2048, 0, }, { 0, 2048, }, { 1024, 1024, },
            { 4096, -2048, }, { 3584, -1536, }, { 3072, -1024, }, { 4608, -2560, },
            { 4200, -2248, }, { 4800, -2300, }, { 5120, -3072, }, { 2048, -2048, },
            { 1024, -1024, }, { -1024, 1024, }, { -1024, 0, }, { -2048, 0, },
        };

        static short ClampSample16Bit(int sample)
        {
            if (sample < -32768)
            {
                sample = -32768;
            }
            else if (sample > 32767)
            {
                sample = 32767;
            }

            return (short)sample;
        }

        public static void PCM16TOADPCM4(short[] pcm, byte[] dst, ref int lastRef, ref int penultRef, bool isForceCoef0 = false)
        {
            byte[] nibble = new byte[16]; // 4-bit nibble
            long leastDiff = long.MaxValue;
            short leastLast = 0;
            short leastPenalt = 0;
            byte leastIndex = 0;
            byte leastScale = 0;

            // init nibble data
            for (int nibiter = 0; nibiter < 16; nibiter++)
                nibble[nibiter] = 0;

            int forceCoef0 = isForceCoef0 ? 1 : 16;

            // create coefs
            for (byte i = 0; i < forceCoef0; i++)
            {

                // create scale
                for (byte c = 0; c < 16; c++)
                {
                    short penult = (short)penultRef;
                    short last = (short)lastRef;
                    long diff = 0;
                    byte[] nibble_tmp = new byte[16];

                    // create sample nibbles
                    for (int s = 0; s < 16; s++)
                    {
                        int decSample_nib = 0;
                        long leastError_nib = long.MaxValue;

                        // quickly predict the best nibble to be used
                        int nibblePredict = convertPcmToNibbleAdpcm4(pcm[s], c, i, last, penult);

                        if (nibblePredict > 7)
                            nibblePredict = 7;
                        else if (nibblePredict < -8)
                            nibblePredict = -8;

                        byte nibble_U8 = (byte)(nibblePredict & 0xF); // convert nibble to 4-bit
                        decSample_nib = decodeSampleAdpcm4(nibble_U8, c, i, last, penult);

                        if (isValidS16Value(decSample_nib))
                        {
                            leastError_nib = Math.Abs(decSample_nib - pcm[s]);
                            nibble_tmp[s] = nibble_U8;
                        }
                        else
                        {
                            // if the prediction failed, find next valid nibble
                            for (byte nib = 0; nib < 16; nib++)
                            {
                                int dec_sample = decodeSampleAdpcm4(nib, c, i, last, penult);

                                if (isValidS16Value(dec_sample))
                                {
                                    long nibDiff = Math.Abs(dec_sample - pcm[s]);

                                    if (nibDiff < leastError_nib)
                                    {
                                        leastError_nib = nibDiff;
                                        nibble_tmp[s] = nib;
                                        decSample_nib = dec_sample;
                                    }
                                }
                            }

                            if (leastError_nib == long.MaxValue)
                            {
                                diff = long.MaxValue; // did not find a valid nibble for this coef and scale
                                break;
                            }
                        }

                        diff += leastError_nib;
                        penult = last;
                        last = (short)decSample_nib;
                    }

                    if (diff < leastDiff)
                    {
                        leastDiff = diff;
                        leastLast = last;
                        leastPenalt = penult;
                        leastIndex = i;
                        leastScale = c;

                        for (int nibiter = 0; nibiter < 16; nibiter++)
                            nibble[nibiter] = nibble_tmp[nibiter];
                    }
                }
            }

            // write header
            dst[0] = (byte)((leastScale << 4) | (leastIndex & 0xF));

            // fill nibble data
            for (int i = 0; i < 8; i++)
                dst[i + 1] = (byte)(((nibble[i * 2] << 4)) | (nibble[i * 2 + 1] & 0xF));

            // write refs
            lastRef = leastLast;
            penultRef = leastPenalt;
        }

        // from TLoZ Wind Waker's DecodeADPCM function
        private static int decodeSampleAdpcm4(byte nibble, byte scale, byte index, short last, short penult)
        {
            return (sSigned4BitTable[nibble] << scale) + ((sAdpcmCoefficents[index, 0] * last + sAdpcmCoefficents[index, 1] * penult) >> 11);
        }

        private static int convertPcmToNibbleAdpcm4(short PCM, byte scale, byte index, short last, short penult)
        {
            int scaleVal = (1 << scale);
            int coeffVal = (PCM - ((sAdpcmCoefficents[index, 0] * last + sAdpcmCoefficents[index, 1] * penult) >> 11));

            double scaleVal_d = (double)(scaleVal);
            double coeffVal_d = (double)(coeffVal);
            double result = coeffVal_d / scaleVal_d;

            if (result < 0.0)
                result -= 0.5;
            else if (result > 0.0)
                result += 0.5;

            return (int)result;
        }

        private static bool isValidS16Value(int i)
        {
            if (i > short.MaxValue)
                return false;
            else if (i < short.MinValue)
                return false;
            else
                return true;
        }



        public static void ADPCM2TOPCM16(byte[] adpcm2, short[] pcm16, ref int last, ref int penult)
        {
            var header = adpcm2[0];
            var nibbleCoeff = (8192 << (header >> 4));

            var coeffIndex = (header & 0xF);
            var lastCoeff = sAdpcmCoefficents[coeffIndex, 0];
            var penultCoeff = sAdpcmCoefficents[coeffIndex, 1];

            for (var i = 0; i < 4; ++i)
            {
                var input = adpcm2[1 + i];

                for (var j = 0; j < 4; ++j)
                {
                    var nibble = sSigned2BitTable[(input >> 6) & 0x3];
                    var sample = ClampSample16Bit(((nibble * nibbleCoeff) + (lastCoeff * last) + (penultCoeff * penult)) >> 11);

                    penult = last;
                    last = sample;
                    pcm16[i * 4 + j] = sample;
                    input <<= 2;
                }
            }
        }

        public static void PCM16TOADPCM2(short[] pcm, byte[] dst, ref int lastRef, ref int penultRef, bool isForceCoef0 = false)
        {
            byte[] nibble = new byte[16]; // 2-bit nibble
            long leastDiff = long.MaxValue;
            short leastLast = 0;
            short leastPenalt = 0;
            byte leastIndex = 0;
            byte leastScale = 0;

            // init nibble data
            for (int nibiter = 0; nibiter < 16; nibiter++)
                nibble[nibiter] = 0;

            int forceCoef0 = isForceCoef0 ? 1 : 16;

            // create coefs
            for (byte i = 0; i < forceCoef0; i++)
            {

                // create scale
                for (byte c = 0; c < 16; c++)
                {
                    short penult = (short)penultRef;
                    short last = (short)lastRef;
                    long diff = 0;
                    byte[] nibble_tmp = new byte[16];

                    // create sample nibbles
                    for (int s = 0; s < 16; s++)
                    {
                        int decSample_nib = 0;
                        long leastError_nib = long.MaxValue;

                        for (byte nib = 0; nib < 4; nib++)
                        {
                            int dec_sample = decodeSampleAdpcm2(nib, c, i, last, penult);

                            if (isValidS16Value(dec_sample))
                            {
                                long nibDiff = Math.Abs(dec_sample - pcm[s]);

                                if (nibDiff < leastError_nib)
                                {
                                    leastError_nib = nibDiff;
                                    nibble_tmp[s] = nib;
                                    decSample_nib = dec_sample;
                                }
                            }
                        }

                        if (leastError_nib == long.MaxValue)
                        {
                            diff = long.MaxValue; // did not find a valid nibble for this coef and scale
                            break;
                        }

                        diff += leastError_nib;
                        penult = last;
                        last = (short)decSample_nib;
                    }

                    if (diff < leastDiff)
                    {
                        leastDiff = diff;
                        leastLast = last;
                        leastPenalt = penult;
                        leastIndex = i;
                        leastScale = c;

                        for (int nibiter = 0; nibiter < 16; nibiter++)
                            nibble[nibiter] = nibble_tmp[nibiter];
                    }
                }
            }

            // write header
            dst[0] = (byte)((leastScale << 4) | (leastIndex & 0xF));

            // fill nibble data
            for (int i = 0; i < 4; i++)
                dst[i + 1] = (byte)((nibble[i * 4] << 6) | ((nibble[i * 4 + 1] & 0x3) << 4) | ((nibble[i * 4 + 2] & 0x3) << 2) | (nibble[i * 4 + 3] & 0x3));

            // write refs
            lastRef = leastLast;
            penultRef = leastPenalt;
        }
        private static int decodeSampleAdpcm2(byte nibble, byte scale, byte index, short last, short penult)
        {
            return ((sSigned2BitTable[nibble] << scale) * 4) + ((sAdpcmCoefficents[index, 0] * last + sAdpcmCoefficents[index, 1] * penult) >> 11);
        }



        public static void ADPCM4TOPCM16(byte[] adpcm4, short[] pcm16, ref int last, ref int penult)
        {
            var header = adpcm4[0];
            var nibbleCoeff = (2048 << (header >> 4));

            var coeffIndex = (header & 0xF);
            var lastCoeff = sAdpcmCoefficents[coeffIndex, 0];
            var penultCoeff = sAdpcmCoefficents[coeffIndex, 1];

            for (var i = 0; i < 8; ++i)
            {
                var input = adpcm4[1 + i];

                for (var j = 0; j < 2; ++j)
                {
                    var nibble = sSigned4BitTable[(input >> 4) & 0xF];
                    var sample = ClampSample16Bit((nibbleCoeff * nibble + lastCoeff * last + penultCoeff * penult) >> 11);

                    penult = last;
                    last = sample;
                    pcm16[i * 2 + j] = sample;
                    input <<= 4;
                }
            }
        }
    }
}


