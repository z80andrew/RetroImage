using System;
using Z80andrew.RetroImage.Common;

namespace Z80andrew.RetroImage.Services
{
    internal class Compression
    {
        public static (int, byte[]) DecompressPackBits(byte[] imageBytes)
        {
            byte[] data = new byte[Constants.SCREEN_MEMORY_BYTES];
            int sourceIndex = 0;
            int destIndex = 0;

            while (destIndex < data.Length)
            {
                var controlByte = imageBytes[sourceIndex];
                sourceIndex++;

                int runLength;
                // RLE run
                if (controlByte > 128)
                {
                    runLength = 257 - controlByte;

                    var rleByte = imageBytes[sourceIndex];

                    for (; runLength > 0; runLength--)
                    {
                        data[destIndex] = rleByte;
                        destIndex++;
                    }

                    sourceIndex++;
                }

                // Use source bytes
                else if (controlByte < 128)
                {
                    runLength = controlByte + 1;

                    for (; runLength > 0; runLength--)
                    {
                        data[destIndex] = imageBytes[sourceIndex];
                        destIndex++;
                        sourceIndex++;
                    }
                }
            }

            return (sourceIndex, data);
        }

        public static byte[] InterleavePlanes(byte[] sequentialPlaneData, int width, int numPlanes)
        {
            byte[] screenMemoryData = new byte[sequentialPlaneData.Length];
            int sourceIndex = 0;

            // Each pixel is 1 bit on a plane, and each word is 16 bits
            int wordsPerBitplaneRow = width / 16;

            int rowStartArrayIndex = 0;

            while (sourceIndex < sequentialPlaneData.Length)
            {
                // Write all bitplanes to a row
                for (int p = 0; p < numPlanes; p++)
                {
                    int bitplaneRowOffset = p * 2;

                    // Write each bitplane row data one word at a time
                    for (int i = 0; i < wordsPerBitplaneRow; i++)
                    {
                        var destination = rowStartArrayIndex + (i * numPlanes * 2) + bitplaneRowOffset;

                        screenMemoryData[destination] = sequentialPlaneData[sourceIndex];
                        sourceIndex++;
                        screenMemoryData[destination + 1] = sequentialPlaneData[sourceIndex];
                        sourceIndex++;
                    }
                }

                // Move destination pointer to the next row
                rowStartArrayIndex += (wordsPerBitplaneRow * 2) * numPlanes;
            }

            return screenMemoryData;
        }
    }
}
