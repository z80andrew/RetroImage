using SixLabors.ImageSharp;
using System;
using System.IO;
using Z80andrew.RetroImage.Helpers;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal class TinyService : AtariImageService
    {
        internal byte PALETTE_OFFSET;
        internal byte BODY_OFFSET;
        internal byte MAX_ANIMATIONS;

        public TinyService()
        {
            Init();
        }

        internal virtual void Init()
        {
            PALETTE_OFFSET = 0x01;
            BODY_OFFSET = 0x21;
            MAX_ANIMATIONS = 0x01;
        }

        internal override (int, byte[]) GetImageBody(Stream imageStream, CompressionType compression, int width, int height, int bitPlanes)
        {
            byte[] imageBytes = new byte[(width * height) / (8 / bitPlanes)];

            imageStream.Position = BODY_OFFSET;

            var numControlBytes = imageStream.ReadByte() << 8 | imageStream.ReadByte();
            var numDataWords = imageStream.ReadByte() << 8 | imageStream.ReadByte();

            var commandBytes = new byte[numControlBytes];
            imageStream.Read(commandBytes, 0, numControlBytes);

            var dataBytes = new byte[numDataWords * 2];
            imageStream.Read(dataBytes, 0, numDataWords * 2);

            // Tiny always uses vertical RLE compression
            imageBytes = Compression.DecompressVerticalRLE(commandBytes, dataBytes);

            // Tiny treats every image as low resolution in regards to vertical interleaving
            imageBytes = Compression.InterleaveVerticalPlanes(imageBytes, 320, 200, 4);

            return ((int)imageStream.Position, imageBytes);
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

        internal override Animation[] GetAnimations(Stream imageStream, byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette)
        {
            throw new NotImplementedException();
        }

        internal override bool ImageHasAnimationData(Stream imageStream, int bodyBytes)
        {
            return false;
        }

        internal override CompressionType GetCompressionType(Stream imageStream)
        {
            return CompressionType.VERTICAL_RLE2;
        }

        internal override (Resolution resolution, int width, int height, int bitPlanes) GetImageProperties(Stream imageStream)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            var resolution = (Resolution)imageStream.ReadByte();

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

            return (resolution, width, height, bitPlanes);
        }
    }
}
