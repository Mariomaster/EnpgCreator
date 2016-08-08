/*
*   This file is part of NSMB Editor 5.
*
*   NSMB Editor 5 is free software: you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   NSMB Editor 5 is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public License
*   along with NSMB Editor 5.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSMBe4
{
    class lz77
    {
        public static void LZ77_Compress_Search(byte[] data, int pos, out int match, out int length)
        {
            int maxMatchDiff = 4096;
            int maxMatchLen = 18;
            match = 0;
            length = 0;

            int start = pos - maxMatchDiff;
            if (start < 0) start = 0;

            for (int thisMatch = start; thisMatch < pos; thisMatch++)
            {
                int thisLength = 0;
                while(thisLength < maxMatchLen
                    && thisMatch + thisLength < pos 
                    && pos + thisLength < data.Length
                    && data[pos+thisLength] == data[thisMatch+thisLength])
                    thisLength++;

                if(thisLength > length)
                {
                    match = thisMatch;
                    length = thisLength;
                }

                //We can't improve the max match length again...
                if(length == maxMatchLen)
                    return;
            }
        }


        public static byte[] LZ77_Compress(byte[] data, bool header = false)
        {
            ByteArrayOutputStream res = new ByteArrayOutputStream();
            if (header) //0x37375A4C
            {
                res.writeByte(0x4C);
                res.writeByte(0x5A);
                res.writeByte(0x37);
                res.writeByte(0x37);
            }

            res.writeInt((data.Length << 8) | 0x10);

            byte[] tempBuffer = new byte[16];

            //Current byte to compress.
            int current = 0;

            while (current < data.Length)
            {
                int tempBufferCursor = 0;
                byte blockFlags = 0;
                for (int i = 0; i < 8; i++)

                {
                    //Not sure if this is needed. The DS probably ignores this data.
                    if (current >= data.Length)
                    {
                        tempBuffer[tempBufferCursor++] = 0;
                        continue;
                    }

                    int searchPos = 0;
                    int searchLen = 0;
                    LZ77_Compress_Search(data, current, out searchPos, out searchLen);
                    int searchDisp = current - searchPos - 1;
                    if (searchLen > 2) //We found a big match, let's write a compressed block.
                    {
                        blockFlags |= (byte)(1 << (7 - i));
                        tempBuffer[tempBufferCursor++] = (byte)((((searchLen - 3) & 0xF) << 4) + ((searchDisp >> 8) & 0xF));
                        tempBuffer[tempBufferCursor++] = (byte)(searchDisp & 0xFF);
                        current += searchLen;
                    }
                    else
                    {
                        tempBuffer[tempBufferCursor++] = data[current++];
                    }
                }

                res.writeByte(blockFlags);
                for (int i = 0; i < tempBufferCursor; i++)
                    res.writeByte(tempBuffer[i]);
            }

            return res.getArray();
        }
    }
}
