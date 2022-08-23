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
    internal class DegasService
    {
        private const byte PALETTE_OFFSET = 0x02;
        private const byte BODY_OFFSET = 0x22;

        private const byte RES_LOW = 0;
        private const byte RES_MED = 1;
        private const byte RES_HIGH = 2;

        public static IBitmap ReadDegasImage(string path)
        {
            bool isCompressed = false;
            int width = 0;
            int height = 0;
            int bitPlanes = 0;
            IBitmap returnImage;

            using (FileStream imageFileStream = File.OpenRead(path))
            {
                byte compression = Convert.ToByte(imageFileStream.ReadByte());

                isCompressed = (compression & 0x80) == 0x80;

                byte resolution = Convert.ToByte(imageFileStream.ReadByte());

                switch(resolution)
                {
                    case RES_LOW:
                        width = 320; 
                        height = 200;
                        bitPlanes = 4;
                    break;
                    case RES_MED:
                        width = 640;
                        height = 200;
                        bitPlanes = 2;
                        break;
                    case RES_HIGH:
                        width = 640;
                        height = 400;
                        bitPlanes = 1;
                        break;
                }

                // Medium res on the Atari is stretched 2x vertically
                int outputHeight = resolution == RES_MED ? height * 2 : height;
                
                using Image<Rgb24> gif = new(width, outputHeight, Color.HotPink);

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
                    imageBytes = Compression.InterleavePlanes(uncompressedImage, width, (byte)bitPlanes);
                }

                int x = 0;
                int y = 0;
                int arrayIndex = 0;
                int yIncrement = resolution == RES_MED ? 2 : 1;

                while (y < outputHeight)
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

                                if (resolution == RES_LOW)
                                {
                                    var pixelByte = Convert.ToByte(
                                        ((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask)
                                        | ((imageBytes[arrayIndex + byteIndex + 2] & bitMask) / bitMask) << 1
                                        | ((imageBytes[arrayIndex + byteIndex + 4] & bitMask) / bitMask) << 2
                                        | ((imageBytes[arrayIndex + byteIndex + 6] & bitMask) / bitMask) << 3);

                                    gif[x, y] = colors[pixelByte];
                                    x++;
                                }

                                else if (resolution == RES_MED)
                                {
                                    var pixelByte = Convert.ToByte(
                                        ((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask)
                                        | ((imageBytes[arrayIndex + byteIndex + 2] & bitMask) / bitMask) << 1);

                                    gif[x, y] = colors[pixelByte];
                                    gif[x, y+1] = colors[pixelByte];
                                    x++;
                                }

                                else if(resolution == RES_HIGH)
                                {
                                    var pixelByte = Convert.ToByte((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask);
                                    gif[x, y] = pixelByte == 0 ? Color.White : Color.Black;
                                    x++;
                                }
                            }
                        }

                        // Advance to next bitplane word
                        arrayIndex += (bitPlanes * 2);
                    }

                    x = 0;
                    y+= yIncrement;
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
