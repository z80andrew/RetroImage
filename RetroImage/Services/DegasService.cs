using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Z80andrew.RetroImage.Helpers;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal class DegasService : AtariImageService
    {
        internal byte PALETTE_OFFSET;
        internal byte BODY_OFFSET;
        internal byte MAX_ANIMATIONS;

        public DegasService()
        {
            Init();
        }

        internal virtual void Init()
        {
            PALETTE_OFFSET = 0x02;
            BODY_OFFSET = 0x22;
            MAX_ANIMATIONS = 0x04;
        }

        internal override CompressionType GetCompressionType(FileStream imageFileStream)
        {
            imageFileStream.Seek(0, SeekOrigin.Begin);
            var compression = imageFileStream.ReadByte();

            return (compression & 0x80) == 0x80 ? CompressionType.PACKBITS : CompressionType.NONE;
        }

        internal override (Resolution resolution, int width, int height, int bitPlanes) GetImageProperties(FileStream imageFileStream)
        {
            imageFileStream.Seek(1, SeekOrigin.Begin);
            var resolution = (Resolution)imageFileStream.ReadByte();

            int width = -1;
            int height = -1;
            int bitPlanes = -1;

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

            return (resolution, width, height, bitPlanes);
        }

        internal override bool ImageHasAnimationData(FileStream imageFileStream, int bodyBytes)
        {
            bool hasValidAnimationData = true;
            imageFileStream.Seek(BODY_OFFSET, SeekOrigin.Begin);
            imageFileStream.Seek(bodyBytes, SeekOrigin.Current);
            int maxAnimationBytes = 0x20;
            bool EOF = false;

            while (!EOF)
            {
                var fileByte = imageFileStream.ReadByte();

                // Valid EOF with animations
                if (fileByte == -1 && maxAnimationBytes == 0) EOF = true;

                // Hit EOF too early or too late
                else if (fileByte == -1
                    || maxAnimationBytes < 0)
                {
                    hasValidAnimationData = false;
                    EOF = true;
                }

                maxAnimationBytes--;
            }

            return hasValidAnimationData;
        }

        internal override (int, byte[]) GetImageBody(FileStream imageFileStream, CompressionType compression, int width, int height, int bitPlanes)
        {
            imageFileStream.Seek(BODY_OFFSET, SeekOrigin.Begin);
            byte[] imageBytes = new byte[(width * height) / (8 / bitPlanes)];
            imageFileStream.Read(imageBytes, 0, imageBytes.Length);
            int bytesRead = SCREEN_MEMORY_BYTES;

            if (compression == CompressionType.PACKBITS)
            {
                (bytesRead, byte[] uncompressedImage) = Compression.DecompressPackBits(imageBytes);
                imageBytes = Compression.InterleavePlanes(uncompressedImage, width, bitPlanes);
            }

            return (bytesRead, imageBytes);
        }

        internal override Color[] GetPalette(FileStream imageFileStream, int bitPlanes)
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

        internal override Animation[] GetAnimations(FileStream imageFileStream, byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette)
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
