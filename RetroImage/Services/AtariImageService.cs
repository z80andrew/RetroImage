﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using Z80andrew.RetroImage.Common;
using Z80andrew.RetroImage.Interfaces;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal abstract class AtariImageService
    {
        internal abstract (int, byte[]) GetImageBody(FileStream imageFileStream, CompressionType compression, int width, int height, int bitPlanes);
        internal abstract Animation[] GetAnimations(FileStream imageFileStream, byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette);
        internal abstract bool ImageHasAnimationData(FileStream imageFileStream, int bodyBytes);
        internal abstract CompressionType GetCompressionType(FileStream imageFileStream);
        internal abstract (Resolution resolution, int width, int height, int bitPlanes) GetImageProperties(FileStream imageFileStream);
        internal abstract Color[] GetPalette(FileStream imageFileStream, int bitPlanes);

        internal IAtariImage GetImage(string path)
        {
            IAtariImage atariImage;

            using (FileStream imageFileStream = File.OpenRead(path))
            {
                var compression = GetCompressionType(imageFileStream);

                (var resolution, var width, var height, var bitPlanes) = GetImageProperties(imageFileStream);

                (var fileBodyByteCount, var imageBody) = GetImageBody(imageFileStream, compression, width, height, bitPlanes);
                var palette = GetPalette(imageFileStream, bitPlanes);
                var degasImage = GetImageFromRawData(width, height, resolution, bitPlanes, palette, imageBody);

                var animations = new Animation[0];

                if (ImageHasAnimationData(imageFileStream, fileBodyByteCount))
                {
                    animations = GetAnimations(imageFileStream, imageBody, width, height, resolution, bitPlanes, palette);
                }

                atariImage = new DegasImageModel()
                {
                    Width = width,
                    Height = height,
                    Resolution = resolution,
                    Compression = compression,
                    NumBitPlanes = bitPlanes,
                    Palette = palette,
                    Image = degasImage,
                    RawData = imageBody,
                    Animations = animations
                };
            }

            return atariImage;
        }

        internal Image<Rgba32> GetImageFromRawData(int width, int height, Constants.Resolution resolution, int bitPlanes, Color[] colors, byte[] imageBytes)
        {
            int x = 0;
            int y = 0;
            int arrayIndex = 0;
            // Medium res on the Atari is stretched 2x vertically            
            int yIncrement = resolution == Resolution.MED ? 2 : 1;
            var outputHeight = resolution == Resolution.MED ? height * 2 : height;

            var outputImage = new Image<Rgba32>(width, outputHeight, RGBA_TRANSPARENT);

            //try
            //{
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