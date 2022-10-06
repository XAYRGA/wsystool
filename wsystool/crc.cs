using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/* Work derived from NullFX.CRC 
 * Author: Steve 
 * Source: https://github.com/nullfx/NullFX.CRC 
 */

/* 
MIT License

Copyright (c) 2017 Steve

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



namespace wsysbuilder
{
   public static class crc32
   {
       static uint[] table;
       public static void reset()
       {
           uint poly = 0xedb88320;
           table = new uint[256];
           uint temp = 0;
           for (uint i = 0; i < table.Length; ++i)
           {
               temp = i;
               for (int j = 8; j > 0; --j)
               {
                   if ((temp & 1) == 1)
                   {
                       temp = (uint)((temp >> 1) ^ poly);
                   }
                   else
                   {
                       temp >>= 1;
                   }
               }
               table[i] = temp;
           }
       }
       public static uint ComputeChecksum(byte[] bytes)
       {
           uint crc = 0xffffffff;
           for (int i = 0; i < bytes.Length; ++i)
           {
               byte index = (byte)(((crc) & 0xff) ^ bytes[i]);
               crc = (uint)((crc >> 8) ^ table[index]);
           }
           return ~crc;
       }

       public static byte[] ComputeChecksumBytes(byte[] bytes)
       {
           return BitConverter.GetBytes(ComputeChecksum(bytes));
       }
   }
}
