using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using Z80andrew.RetroImage.Interfaces;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    public class DegasService : IAtariImageService
    {
        protected byte PALETTE_OFFSET;
        protected byte BODY_OFFSET;
        protected byte MAX_ANIMATIONS;

        public DegasService()
        {
            Init();
        }

        protected virtual void Init()
        {
            PALETTE_OFFSET = 0x02;
            BODY_OFFSET = 0x22;
            MAX_ANIMATIONS = 0x04;
        }

        public IAtariImage GetImage(string path)
        {
            IAtariImage atariImage;

            using (FileStream imageFileStream = File.OpenRead(path))
            {
                int width = 0;
                int height = 0;
                int bitPlanes = 0;

                byte compression = Convert.ToByte(imageFileStream.ReadByte());

                var isCompressed = (compression & 0x80) == 0x80;

                var resolution = (Resolution)imageFileStream.ReadByte();

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

                (var fileBodyByteCount, var imageBody) = GetDegasImageBody(imageFileStream, isCompressed, width, height, bitPlanes);                
                var palette = GetDegasPalette(imageFileStream, bitPlanes);
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
                    IsCompressed = isCompressed,
                    NumBitPlanes = bitPlanes,
                    Palette = palette,
                    Image = degasImage,
                    RawData = imageBody,
                    Animations = animations
                };
            }
            
            return atariImage;
        }

        private bool ImageHasAnimationData(FileStream imageFileStream, int bodyBytes)
        {
            imageFileStream.Seek(BODY_OFFSET, SeekOrigin.Begin);
            imageFileStream.Seek(bodyBytes, SeekOrigin.Current);
            return imageFileStream.ReadByte() != -1;
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

        public (int, byte[]) GetDegasImageBody(FileStream imageFileStream, bool isCompressed, int width, int height, int bitPlanes)
        {
            imageFileStream.Seek(BODY_OFFSET, SeekOrigin.Begin);
            byte[] imageBytes = new byte[(width * height) / (8 / bitPlanes)];
            imageFileStream.Read(imageBytes, 0, imageBytes.Length);
            int bytesRead = SCREEN_MEMORY_BYTES;

            if (isCompressed)
            {
                (bytesRead, byte[] uncompressedImage) = Compression.DecompressPackBits(imageBytes);
                imageBytes = Compression.InterleavePlanes(uncompressedImage, width, bitPlanes);
            }

            return (bytesRead, imageBytes);
        }

        public Color[] GetDegasPalette(FileStream imageFileStream, int bitPlanes)
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

        public Animation[] GetAnimations(FileStream imageFileStream, byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette)
        {
            var animations = new List<Animation>();

            for (int animationIndex = 0; animationIndex < MAX_ANIMATIONS; animationIndex++)
            {
                imageFileStream.Seek(-0x20 + (animationIndex * 2), SeekOrigin.End);

                imageFileStream.Seek(1, SeekOrigin.Current);
                var lowerPaletteIndex = imageFileStream.ReadByte();

                imageFileStream.Seek(7, SeekOrigin.Current);
                var upperPaletteIndex = imageFileStream.ReadByte();

                imageFileStream.Seek(7, SeekOrigin.Current);
                var animationDirection = (AnimationDirection)imageFileStream.ReadByte();

                imageFileStream.Seek(7, SeekOrigin.Current);
                var animationDelay = imageFileStream.ReadByte();

                if (animationDirection != AnimationDirection.None)
                {
                    animations.Add(new Animation(imageBody, width, height, resolution, numBitPlanes, palette, lowerPaletteIndex, upperPaletteIndex, animationIndex)
                    {
                        Direction = animationDirection,
                        Delay = (float)(1000 / 60) * (128 - animationDelay)
                    });
                }
            }

            return animations.ToArray();
        }
    }
}
