using DynamicData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Z80andrew.RetroImage.Common;
using Z80andrew.RetroImage.Models;

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

        public static byte[] DecompressVerticalRLE(VerticalRleModel[] verticalRleData)
        {
            var output = new List<byte>();

            foreach (var verticalRlePlane in verticalRleData)
            {
                int dataIndex = 0;
                int commandIndex = 0;

                while (dataIndex < verticalRlePlane.DataBytes.Length)
                {
                    var controlByte = (sbyte)verticalRlePlane.CommandBytes[commandIndex];
                    commandIndex++;

                    if (controlByte == 0)
                    {
                        var outputLength = verticalRlePlane.DataBytes[dataIndex++] << 8 | verticalRlePlane.DataBytes[dataIndex++];

                        while (outputLength > 0)
                        {
                            output.Add(verticalRlePlane.DataBytes[dataIndex++]);
                            output.Add(verticalRlePlane.DataBytes[dataIndex++]);
                            outputLength--;
                        }
                    }

                    else if (controlByte == 1)
                    {
                        var runLength = verticalRlePlane.DataBytes[dataIndex++] << 8 | verticalRlePlane.DataBytes[dataIndex++];

                        var rleByte1 = verticalRlePlane.DataBytes[dataIndex++];
                        var rleByte2 = verticalRlePlane.DataBytes[dataIndex++];

                        while (runLength > 0)
                        {
                            output.Add(rleByte1);
                            output.Add(rleByte2);
                            runLength--;
                        }
                    }

                    else if (controlByte < 0)
                    {
                        var outputLength = controlByte * -1;

                        while (outputLength > 0)
                        {
                            output.Add(verticalRlePlane.DataBytes[dataIndex++]);
                            output.Add(verticalRlePlane.DataBytes[dataIndex++]);
                            outputLength--;
                        }
                    }

                    else if (controlByte > 1)
                    {
                        var runLength = controlByte;

                        var rleByte1 = verticalRlePlane.DataBytes[dataIndex++];
                        var rleByte2 = verticalRlePlane.DataBytes[dataIndex++];

                        while (runLength > 0)
                        {
                            output.Add(rleByte1);
                            output.Add(rleByte2);
                            runLength--;
                        }
                    }
                }

                Debug.WriteLine(output.Count);
            }

            return output.ToArray();
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

        public static byte[] InterleaveVerticalPlanes(byte[] sequentialPlaneData, int width, int height, int numPlanes)
        {
            byte[] screenMemoryData = new byte[32000]; //new byte[sequentialPlaneData.Length];

            try
            {
                // Each pixel is 1 bit on a plane, and each word is 16 bits
                int wordsPerBitplaneRow = width / 16;

                for (int p = 0; p < numPlanes; p++)
                {
                    int destinationOffset = p * 2;
                    var sourceOffset = p * (Constants.SCREEN_MEMORY_BYTES / 4);

                    var destinationIndex = destinationOffset;

                    for (int y = 0; y < height; y++)
                    {
                        var sourceIndex = sourceOffset + (y * 2);

                        for (int i = 0; i < wordsPerBitplaneRow; i++)
                        {
                            screenMemoryData[destinationIndex] = sequentialPlaneData[sourceIndex];
                            screenMemoryData[destinationIndex + 1] = sequentialPlaneData[sourceIndex + 1];

                            destinationIndex += (2 * numPlanes);
                            sourceIndex += 2 * height;
                        }
                    }
                }
            }

            catch
            {
                Debug.WriteLine("Error arranging data");
            }

            return screenMemoryData;
        }

        internal static byte[] DecompressVerticalRLE(byte[] commandBytes, byte[] dataBytes)
        {
            var output = new List<byte>();


            int dataIndex = 0;
            int commandIndex = 0;

            while (commandIndex < commandBytes.Length)
            {
                var controlByte = (sbyte)commandBytes[commandIndex];
                commandIndex++;

                if (controlByte == 1)
                { 
                    var outputLength = commandBytes[commandIndex++] << 8 | commandBytes[commandIndex++];

                    while (outputLength > 0)
                    {
                        output.Add(dataBytes[dataIndex++]);
                        output.Add(dataBytes[dataIndex++]);
                        outputLength--;
                    }
                }

                else if (controlByte == 0)
                {
                    var runLength = commandBytes[commandIndex++] << 8 | commandBytes[commandIndex++];

                    var rleByte1 = dataBytes[dataIndex++];
                    var rleByte2 = dataBytes[dataIndex++];

                    while (runLength > 0)
                    {
                        output.Add(rleByte1);
                        output.Add(rleByte2);
                        runLength--;
                    }
                }

                else if (controlByte < 0)
                {
                    var outputLength = controlByte * -1;

                    while (outputLength > 0)
                    {
                        output.Add(dataBytes[dataIndex++]);
                        output.Add(dataBytes[dataIndex++]);
                        outputLength--;
                    }
                }

                else if (controlByte > 1)
                {
                    var runLength = controlByte;

                    var rleByte1 = dataBytes[dataIndex++];
                    var rleByte2 = dataBytes[dataIndex++];

                    while (runLength > 0)
                    {
                        output.Add(rleByte1);
                        output.Add(rleByte2);
                        runLength--;
                    }
                }
            }

            return output.Take(Constants.SCREEN_MEMORY_BYTES).ToArray();
        }
    }
}
