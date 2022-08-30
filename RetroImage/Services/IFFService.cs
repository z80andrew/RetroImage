using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z80andrew.RetroImage.Common;
using Z80andrew.RetroImage.Interfaces;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal class IFFService : IAtariImageService
    {
        private const string CHUNK_ID_FORM = "FORM";
        private const string CHUNK_ID_INTERLEAVED_BITMAP = "ILBM";
        private const string CHUNK_ID_BITMAP_HEADER = "BMHD";
        private const string CHUNK_ID_COLORMAP = "CMAP";
        private const string CHUNK_ID_COLOR_RANGE = "CRNG";
        private const string CHUNK_ID_AMIGA_VIEWPORT = "CAMG";
        private const string CHUNK_ID_BODY = "BODY";
        private const string CHUNK_ID_VERTICAL_DATA = "VDAT";

        public IAtariImage GetImage(string path)
        {
            IAtariImage atariImage;

            using (FileStream imageFileStream = File.OpenRead(path))
            {
                var palette = GetPalette(imageFileStream);
                (var width, var height, var bitPlanes, var compression) = SetImageDimensions(imageFileStream);
                (var fileBodyByteCount, var imageBody) = GetImageBody(imageFileStream, compression, width, height, bitPlanes);
                var image = GetImageFromRawData(width, height, Resolution.LOW, bitPlanes, palette, imageBody);

                atariImage = new DegasImageModel()
                {
                    Width = width,
                    Height = height,
                    Resolution = Resolution.LOW,
                    Compression = compression,
                    NumBitPlanes = bitPlanes,
                    Palette = palette,
                    Image = image,
                    RawData = imageBody,
                    Animations = new Animation[0]
                };
            }

            return atariImage;
        }

        public (int, byte[]) GetImageBody(FileStream imageFileStream, CompressionType compression, int width, int height, int bitPlanes)
        {
            byte[] imageBytes = new byte[(width * height) / (8 / bitPlanes)];

            var bodyOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_BODY);
            imageFileStream.Seek(bodyOffset, SeekOrigin.Begin);

            imageFileStream.Read(imageBytes, 0, imageBytes.Length);
            int bytesRead = (width * height * bitPlanes) / 8;

            if (compression == CompressionType.PACKBITS)
            {
                (bytesRead, byte[] uncompressedImage) = Compression.DecompressPackBits(imageBytes);
                imageBytes = Compression.InterleavePlanes(uncompressedImage, width, bitPlanes);
            }

            else if (compression == CompressionType.VERTICAL_RLE1)
            {
                var verticalRleData = new VerticalRleModel[bitPlanes];

                int vdatChunkOffset = 0;
                var vdatOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_VERTICAL_DATA, vdatChunkOffset);
                imageFileStream.Seek(vdatOffset, SeekOrigin.Begin);

                for (int bitPlane = 0; bitPlane < bitPlanes; bitPlane++)
                {
                    // Move past chunk ID
                    imageFileStream.Seek(4, SeekOrigin.Current);

                    var chunkLength = (Convert.ToByte(imageFileStream.ReadByte()) << 24
                        | Convert.ToByte(imageFileStream.ReadByte()) << 16
                        | Convert.ToByte(imageFileStream.ReadByte()) << 8
                        | Convert.ToByte(imageFileStream.ReadByte()));

                    chunkLength -= 4;

                    // Number of command bytes read is always 2 more than actually available
                    var numCommandBytes = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte() - 2;

                    var commandBytes = new byte[numCommandBytes];
                    imageFileStream.Read(commandBytes, 0, numCommandBytes);

                    var numDataBytes = chunkLength - numCommandBytes + 2;

                    var dataBytes = new byte[numDataBytes];
                    imageFileStream.Read(dataBytes, 0, numDataBytes);

                    verticalRleData[bitPlane] = new VerticalRleModel() { CommandBytes = commandBytes, DataBytes = dataBytes };
                }

                imageBytes = Compression.DecompressVerticalRLE(verticalRleData);
                imageBytes = Compression.InterleaveVerticalPlanes(imageBytes, width, bitPlanes);
            }

            return (bytesRead, imageBytes);
        }

        public Color[] GetPalette(FileStream imageFileStream)
        {
            var paletteOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_COLORMAP);

            // Offset + chunk header + first 3 bytes of longword
            imageFileStream.Seek(paletteOffset + 4 + 3, SeekOrigin.Begin);

            var numColors = imageFileStream.ReadByte() / 3;

            var colors = new Color[numColors];

            for (int cIndex = 0; cIndex < colors.Length; cIndex++)
            {
                // RGB are stored as 3-bit values, i.e. there are 7 possible RGB levels
                var r = Convert.ToByte(imageFileStream.ReadByte());
                var g = Convert.ToByte(imageFileStream.ReadByte());
                var b = Convert.ToByte(imageFileStream.ReadByte());

                colors[cIndex] = Color.FromRgb(r, g, b);
            }

            return colors;
        }


        protected virtual (int width, int height, int bitPlanes, CompressionType compression) SetImageDimensions(FileStream imageFileStream)
        {
            var headerOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_BITMAP_HEADER);

            imageFileStream.Seek(headerOffset + 8, SeekOrigin.Begin);

            int width = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
            int height = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
            imageFileStream.Seek(4, SeekOrigin.Current);
            int bitPlanes = imageFileStream.ReadByte();
            imageFileStream.Seek(1, SeekOrigin.Current);
            var compression = (CompressionType)imageFileStream.ReadByte();

            return (width, height, bitPlanes, compression);
        }

        public Image<Rgba32> GetImageFromRawData(int width, int height, Constants.Resolution resolution, int bitPlanes, Color[] colors, byte[] imageBytes)
        {
            int x = 0;
            int y = 0;
            int arrayIndex = 0;
            // Medium res on the Atari is stretched 2x vertically            
            int yIncrement = resolution == Resolution.MED ? 2 : 1;
            var outputHeight = resolution == Resolution.MED ? height * 2 : height;

            var degasImage = new Image<Rgba32>(width, outputHeight, RGBA_TRANSPARENT);

            try
            {
                while (y < outputHeight)
                {
                    while (x < width)
                    {
                        // Each bitplane has 1 word (2 bytes) of contiguous image data
                        for (int byteIndex = 0; byteIndex < 2; byteIndex++)
                        {
                            for (int bitIndex = 7; bitIndex >= 0; bitIndex--)
                            {
                                byte bitMask = (byte)Math.Pow(2, bitIndex);

                                if (resolution == Resolution.LOW)
                                {
                                    var pixelByte = Convert.ToByte(
                                        ((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask)
                                        | ((imageBytes[arrayIndex + byteIndex + 2] & bitMask) / bitMask) << 1
                                        | ((imageBytes[arrayIndex + byteIndex + 4] & bitMask) / bitMask) << 2
                                        | ((imageBytes[arrayIndex + byteIndex + 6] & bitMask) / bitMask) << 3);

                                    degasImage[x, y] = colors[pixelByte];
                                    x++;
                                }

                                else if (resolution == Resolution.MED)
                                {
                                    var pixelByte = Convert.ToByte(
                                        ((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask)
                                        | ((imageBytes[arrayIndex + byteIndex + 2] & bitMask) / bitMask) << 1);

                                    degasImage[x, y] = colors[pixelByte];
                                    degasImage[x, y + 1] = colors[pixelByte];
                                    x++;
                                }

                                else if (resolution == Resolution.HIGH)
                                {
                                    var pixelByte = Convert.ToByte((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask);
                                    degasImage[x, y] = pixelByte == 0 ? Color.White : Color.Black;
                                    x++;
                                }
                            }
                        }

                        // Advance to next bitplane word
                        arrayIndex += (bitPlanes * 2);
                    }

                    x = 0;
                    y += yIncrement;
                }
            }

            catch { }

            return degasImage;
        }

        private int GetChunkOffset(string filePath, string chunkID, int startIndex = 0)
        {
            int chunkOffset = -1;

            using (TextReader reader = new StreamReader(filePath, Encoding.ASCII))
            {
                var fileContents = reader.ReadToEnd();
                chunkOffset = fileContents.IndexOf(chunkID, startIndex);
            }

            return chunkOffset;
        }
    }
}
