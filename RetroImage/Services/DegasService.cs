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
        internal byte RESOLUTION_OFFSET;
        internal byte PALETTE_OFFSET;
        internal byte BODY_OFFSET;
        internal byte MAX_ANIMATIONS;

        public DegasService()
        {
            Init();
        }

        internal virtual void Init()
        {
            RESOLUTION_OFFSET = 0x01;
            PALETTE_OFFSET = 0x02;
            BODY_OFFSET = 0x22;
            MAX_ANIMATIONS = 0x04;
        }

        internal override CompressionType GetCompressionType(Stream imageStream)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            var compression = imageStream.ReadByte();

            return (compression & 0x80) == 0x80 ? CompressionType.PACKBITS : CompressionType.NONE;
        }

        internal override (Resolution resolution, int width, int height, int renderHeight, int bitPlanes) GetImageProperties(Stream imageStream)
        {
            imageStream.Seek(RESOLUTION_OFFSET, SeekOrigin.Begin);
            var resolution = (Resolution)imageStream.ReadByte();

            (var width, var height, var renderHeight, var numBitPlanes) = GetResolutionSettings(resolution);

            return (resolution, width, height, renderHeight, numBitPlanes);
        }

        internal override bool ImageHasAnimationData(Stream imageStream, int bodyBytes)
        {
            bool hasValidAnimationData = true;
            imageStream.Seek(BODY_OFFSET, SeekOrigin.Begin);
            imageStream.Seek(bodyBytes, SeekOrigin.Current);
            int maxAnimationBytes = 0x20;
            bool EOF = false;

            while (!EOF)
            {
                var fileByte = imageStream.ReadByte();

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

        internal override (int, byte[]) GetImageBody(Stream imageStream, CompressionType compression, int width, int height, int bitPlanes)
        {
            imageStream.Seek(BODY_OFFSET, SeekOrigin.Begin);
            byte[] imageBytes = new byte[(width * height) / (8 / bitPlanes)];
            imageStream.Read(imageBytes, 0, imageBytes.Length);
            int bytesRead = SCREEN_MEMORY_BYTES;

            if (compression == CompressionType.PACKBITS)
            {
                (bytesRead, byte[] uncompressedImage) = Compression.DecompressPackBits(imageBytes);
                imageBytes = Compression.InterleavePlanes(uncompressedImage, width, bitPlanes);
            }

            return (bytesRead, imageBytes);
        }

        internal override Color[] GetPalette(Stream imageStream, int bitPlanes)
        {
            var colors = new Color[(int)Math.Pow(2, bitPlanes)];

            imageStream.Seek(PALETTE_OFFSET, SeekOrigin.Begin);

            for (int cIndex = 0; cIndex < colors.Length; cIndex++)
            {
                int v = imageStream.ReadByte() << 8 | imageStream.ReadByte();
                // RGB are stored as 3-bit values, i.e. there are 7 possible RGB levels
                var b = Convert.ToByte(((v >> 0) & 0x07) * (255 / 7));
                var g = Convert.ToByte(((v >> 4) & 0x07) * (255 / 7));
                var r = Convert.ToByte(((v >> 8) & 0x07) * (255 / 7));

                colors[cIndex] = Color.FromRgb(r, g, b);
            }

            return colors;
        }

        internal override Animation[] GetAnimations(Stream imageStream, byte[] imageBody, int width, int height, int renderHeight, Resolution resolution, int numBitPlanes, Color[] palette)
        {
            var animations = new List<Animation>();

            for (int animationIndex = 0; animationIndex < MAX_ANIMATIONS; animationIndex++)
            {
                imageStream.Seek(imageStream.Length - 0x20 + (animationIndex * 2), SeekOrigin.Begin);

                imageStream.Seek(1, SeekOrigin.Current);
                var lowerPaletteIndex = imageStream.ReadByte();

                imageStream.Seek(7, SeekOrigin.Current);
                var upperPaletteIndex = imageStream.ReadByte();

                imageStream.Seek(7, SeekOrigin.Current);
                var animationDirection = (AnimationDirection)imageStream.ReadByte();

                imageStream.Seek(7, SeekOrigin.Current);
                var animationDelay = imageStream.ReadByte();

                if (animationDirection != AnimationDirection.None)
                {
                    animations.Add(new Animation(imageBody, width, height, renderHeight, resolution, numBitPlanes, palette, lowerPaletteIndex, upperPaletteIndex, animationIndex)
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
