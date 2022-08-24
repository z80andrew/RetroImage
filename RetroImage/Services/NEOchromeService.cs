using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z80andrew.RetroImage.Services
{
    internal class NEOchromeService
    {
        private const byte PALETTE_OFFSET = 0x04;
        private const byte BODY_OFFSET = 0x80;

        private const byte RES_LOW = 0;
        private const byte RES_MED = 1;
        private const byte RES_HIGH = 2;

        public static IBitmap ReadNEOImage(string path)
        {
            bool isCompressed = false;
            int width = 320;
            int height = 200;
            int bitPlanes = 4;
            IBitmap returnImage;

            using (FileStream imageFileStream = File.OpenRead(path))
            {
                using Image<Rgb24> gif = new(width, height, Color.HotPink);

                List<Color> colors = new List<Color>();

                imageFileStream.Seek(PALETTE_OFFSET, SeekOrigin.Begin);

                for (int cIndex = 0; cIndex < 16; cIndex++)
                {
                    int v = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
                    // RGB are stored as 3-bit values, i.e. there are 7 possible RGB levels
                    var b = Convert.ToByte(((v >> 0) & 0x07) * (255 / 7));
                    var g = Convert.ToByte(((v >> 4) & 0x07) * (255 / 7));
                    var r = Convert.ToByte(((v >> 8) & 0x07) * (255 / 7));

                    Color c = Color.FromRgb(r, g, b);
                    colors.Add(c);
                }

                imageFileStream.Seek(BODY_OFFSET, SeekOrigin.Begin);
                byte[] imageBytes = new byte[(width * height) / (8/bitPlanes)];
                int readLength = imageFileStream.Read(imageBytes, 0, imageBytes.Length);

                if (isCompressed)
                {
                    var uncompressedImage = Compression.DecompressPackBits(imageBytes);
                    imageBytes = Compression.InterleavePlanes(uncompressedImage, width, bitPlanes);
                }

                int x = 0;
                int y = 0;
                int arrayIndex = 0;

                while (y < height)
                {
                    while (x < width)
                    {
                        // Each bitplane has 1 word (2 bytes) of contiguous image data
                        for (int byteIndex = 0; byteIndex < 2; byteIndex++)
                        {
                            for (int bitIndex = 7; bitIndex >= 0; bitIndex--)
                            {
                                ushort bitMask = 1;

                                for (int b = 1; b <= bitIndex; b++)
                                {
                                    bitMask *= 2;
                                }

                                var pixelByte = Convert.ToByte(
                                    ((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask)
                                    | ((imageBytes[arrayIndex + byteIndex + 2] & bitMask) / bitMask) << 1
                                    | ((imageBytes[arrayIndex + byteIndex + 4] & bitMask) / bitMask) << 2
                                    | ((imageBytes[arrayIndex + byteIndex + 6] & bitMask) / bitMask) << 3);

                                gif[x, y] = colors[pixelByte];
                                x++;

                            }
                        }

                        // Advance to next bitplane word
                        arrayIndex += (bitPlanes * 2);
                    }

                    x = 0;
                    y++;
                }

                using (var imageStream = new MemoryStream())
                {
                    gif.Save(imageStream, PngFormat.Instance);
                    imageStream.Position = 0;
                    returnImage = new Bitmap(imageStream);
                }
            }

            return returnImage;
        }
    }
}
