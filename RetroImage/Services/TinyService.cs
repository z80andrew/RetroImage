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
    internal class TinyService : IAtariImageService
    {
        protected byte PALETTE_OFFSET;
        protected byte BODY_OFFSET;
        protected byte MAX_ANIMATIONS;

        public TinyService()
        {
            Init();
        }

        protected virtual void Init()
        {
            PALETTE_OFFSET = 0x01;
            BODY_OFFSET = 0x21;
            MAX_ANIMATIONS = 0x01;
        }

        public IAtariImage GetImage(string path)
        {
            IAtariImage atariImage;

            using (FileStream imageFileStream = File.OpenRead(path))
            {
                (var width, var height, var bitPlanes) = SetImageDimensions(imageFileStream);
                var palette = GetPalette(imageFileStream, bitPlanes);
                (var fileBodyByteCount, var imageBody) = GetImageBody(imageFileStream, width, height, bitPlanes);
                var image = GetImageFromRawData(width, height, Resolution.LOW, bitPlanes, palette, imageBody);

                atariImage = new DegasImageModel()
                {
                    Width = width,
                    Height = height,
                    Resolution = Resolution.LOW,
                    Compression = CompressionType.VERTICAL_RLE2,
                    NumBitPlanes = bitPlanes,
                    Palette = palette,
                    Image = image,
                    RawData = imageBody,
                    Animations = new Animation[0]
                };
            }

            return atariImage;
        }

        public (int, byte[]) GetImageBody(FileStream imageFileStream, int width, int height, int bitPlanes)
        {
            byte[] imageBytes = new byte[(width * height) / (8 / bitPlanes)];

            imageFileStream.Position = BODY_OFFSET;
                
            var numControlBytes = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
            var numDataWords = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();

            var commandBytes = new byte[numControlBytes];
            imageFileStream.Read(commandBytes, 0, numControlBytes);

            var dataBytes = new byte[numDataWords*2];
            imageFileStream.Read(dataBytes, 0, numDataWords*2);

            imageBytes = Compression.DecompressVerticalRLE(commandBytes, dataBytes);
            imageBytes = Compression.InterleaveVerticalPlanes(imageBytes, width, bitPlanes);

            return ((int)imageFileStream.Position, imageBytes);
        }

        public Color[] GetPalette(FileStream imageFileStream, int bitPlanes)
        {
            var colors = new Color[(int)Math.Pow(2, bitPlanes)];

            imageFileStream.Seek(PALETTE_OFFSET, SeekOrigin.Begin);

            for (int cIndex = 0; cIndex < colors.Length; cIndex++)
            {
                int v = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
                // RGB are stored as 3-bit values, i.e. there are 7 possible RGB levels
                var b = Convert.ToByte(((v >> 0) & 0x07) * (255 / 7));
                var g = Convert.ToByte(((v >> 4) & 0x07) * (255 / 7));
                var r = Convert.ToByte(((v >> 8) & 0x07) * (255 / 7));

                colors[cIndex] = Color.FromRgb(r, g, b);
            }

            return colors;
        }


        protected virtual (int width, int height, int bitPlanes) SetImageDimensions(FileStream imageFileStream)
        {
                imageFileStream.Position = 0;

                var resolution = (Resolution)imageFileStream.ReadByte();

                int width = 0;
                int height = 0;
                int bitPlanes = 0;

                switch (resolution)
                {
                    case Resolution.LOW:
                        width = 320;
                        height = 200;
                        bitPlanes = 4;
                        break;
                    case Resolution.MED:
                        width = 640;
                        height = 200;
                        bitPlanes = 2;
                        break;
                    case Resolution.HIGH:
                        width = 640;
                        height = 400;
                        bitPlanes = 1;
                        break;
                }

                return (width, height, bitPlanes);
        }

        public Image<Rgba32> GetImageFromRawData(int width, int height, Resolution resolution, int bitPlanes, Color[] colors, byte[] imageBytes)
        {
            int x = 0;
            int y = 0;
            int arrayIndex = 0;
            // Medium res on the Atari is stretched 2x vertically            
            int yIncrement = resolution == Resolution.MED ? 2 : 1;
            var outputHeight = resolution == Resolution.MED ? height * 2 : height;

            var degasImage = new Image<Rgba32>(width, outputHeight, RGBA_TRANSPARENT);

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

            return degasImage;
        }
    }
}
