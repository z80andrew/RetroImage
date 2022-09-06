using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using Z80andrew.RetroImage.Common;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal abstract class AtariImageService
    {
        internal abstract (int, byte[]) GetImageBody(Stream imageStream, CompressionType compression, int width, int height, int bitPlanes);
        internal abstract Animation[] GetAnimations(Stream imageStream, byte[] imageBody, int width, int height, int renderHeight, Resolution resolution, int numBitPlanes, Color[] palette);
        internal abstract bool ImageHasAnimationData(Stream imageStream, int bodyBytes);
        internal abstract CompressionType GetCompressionType(Stream imageStream);
        internal abstract (Resolution resolution, int width, int height, int renderHeight, int bitPlanes) GetImageProperties(Stream imageStream);
        internal abstract Color[] GetPalette(Stream imageStream, int bitPlanes);

        internal (int width, int height, int renderHeight, int numBitPlanes) GetResolutionSettings(Resolution resolution)
        {
            var width = -1;
            var height = -1;
            var renderHeight = -1;
            var numBitPlanes = -1;

            switch (resolution)
            {
                case Resolution.LOW:
                    width = 320;
                    height = 200;
                    numBitPlanes = 4;
                    renderHeight = height;
                    break;
                case Resolution.MED:
                    width = 640;
                    height = 200;
                    numBitPlanes = 2;
                    renderHeight = height*2;
                    break;
                case Resolution.HIGH:
                    width = 640;
                    height = 400;
                    numBitPlanes = 1;
                    renderHeight = height;
                    break;
            }

            return (width, height, renderHeight, numBitPlanes);
        }

        internal AtariImageModel GetImage(string path)
        {
            AtariImageModel atariImage;
            
            using (Stream imageStream = File.OpenRead(path))
            {
                atariImage = GetImage(imageStream, path);
            }

            return atariImage;
        }

        internal AtariImageModel GetImage(Stream imageStream, string fileName)
        {
            (var resolution, var width, var height, var renderHeight, var bitPlanes) = GetImageProperties(imageStream);

            var compression = GetCompressionType(imageStream);
            var palette = GetPalette(imageStream, bitPlanes);
            (var fileBodyByteCount, var imageBody) = GetImageBody(imageStream, compression, width, height, bitPlanes);
            var degasImage = GetImageFromRawData(width, height, renderHeight, resolution, bitPlanes, palette, imageBody);

            var animations = new Animation[0];

            if (ImageHasAnimationData(imageStream, fileBodyByteCount))
            {
                animations = GetAnimations(imageStream, imageBody, width, height, renderHeight, resolution, bitPlanes, palette);
            }

            var atariImage = new AtariImageModel()
            {
                Name = Path.GetFileNameWithoutExtension(fileName),
                Width = width,
                Height = height,
                RenderHeight = renderHeight,
                Resolution = resolution,
                Compression = compression,
                NumBitPlanes = bitPlanes,
                Palette = palette,
                Image = degasImage,
                RawData = imageBody,
                Animations = animations
            };

            return atariImage;
        }

        internal Image<Rgba32> GetImageFromRawData(int width, int height, int renderHeight, Constants.Resolution resolution, int bitPlanes, Color[] colors, byte[] imageBytes)
        {
            int x = 0;
            int y = 0;
            int arrayIndex = 0;
            int yIncrement = renderHeight/height;

            var outputImage = new Image<Rgba32>(width, renderHeight, RGBA_TRANSPARENT);

            //try
            //{
            while (y < renderHeight)
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

                                outputImage[x, y] = colors[pixelByte];
                                x++;
                            }

                            else if (resolution == Resolution.MED)
                            {
                                var pixelByte = Convert.ToByte(
                                    ((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask)
                                    | ((imageBytes[arrayIndex + byteIndex + 2] & bitMask) / bitMask) << 1);

                                outputImage[x, y] = colors[pixelByte];
                                outputImage[x, y + 1] = colors[pixelByte];
                                x++;
                            }

                            else if (resolution == Resolution.HIGH)
                            {
                                var pixelByte = Convert.ToByte((imageBytes[arrayIndex + byteIndex] & bitMask) / bitMask);
                                outputImage[x, y] = pixelByte == 0 ? Color.White : Color.Black;
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
            //}

            //catch { }
            return outputImage;
        }
    }
}
