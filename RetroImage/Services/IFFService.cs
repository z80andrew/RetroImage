using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Z80andrew.RetroImage.Helpers;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal class IFFService : AtariImageService
    {
        private const string CHUNK_ID_FORM = "FORM";
        private const string CHUNK_ID_INTERLEAVED_BITMAP = "ILBM";
        private const string CHUNK_ID_BITMAP_HEADER = "BMHD";
        private const string CHUNK_ID_COLORMAP = "CMAP";
        private const string CHUNK_ID_COLOR_RANGE = "CRNG";
        private const string CHUNK_ID_AMIGA_VIEWPORT = "CAMG";
        private const string CHUNK_ID_BODY = "BODY";
        private const string CHUNK_ID_VERTICAL_DATA = "VDAT";

        internal override CompressionType GetCompressionType(FileStream imageFileStream)
        {
            var headerOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_BITMAP_HEADER);
            imageFileStream.Seek(headerOffset + 18, SeekOrigin.Begin);
            var compression = (CompressionType)imageFileStream.ReadByte();

            return compression;
        }

        internal override (int, byte[]) GetImageBody(FileStream imageFileStream, CompressionType compression, int width, int height, int bitPlanes)
        {
            byte[] imageBytes = new byte[(width * height) / (8 / bitPlanes)];

            var bodyOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_BODY);
            imageFileStream.Seek(bodyOffset, SeekOrigin.Begin);

            imageFileStream.Read(imageBytes, 0, imageBytes.Length);
            int bytesRead = (width * height * bitPlanes) / 8;

            if (compression == CompressionType.PACKBITS)
            {
                var vdatOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_BODY, 0);
                imageFileStream.Seek(vdatOffset, SeekOrigin.Begin);

                imageFileStream.Seek(4, SeekOrigin.Current);

                var chunkLength = (Convert.ToByte(imageFileStream.ReadByte()) << 24
                    | Convert.ToByte(imageFileStream.ReadByte()) << 16
                    | Convert.ToByte(imageFileStream.ReadByte()) << 8
                    | Convert.ToByte(imageFileStream.ReadByte()));

                imageFileStream.Read(imageBytes, 0, chunkLength);

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
                imageBytes = Compression.InterleaveVerticalPlanes(imageBytes, width, height, bitPlanes);
            }

            return (bytesRead, imageBytes);
        }

        internal override (Resolution resolution, int width, int height, int bitPlanes) GetImageProperties(FileStream imageFileStream)
        {
            var headerOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_BITMAP_HEADER);

            imageFileStream.Seek(headerOffset + 8, SeekOrigin.Begin);

            int width = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
            int height = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
            imageFileStream.Seek(4, SeekOrigin.Current);
            int bitPlanes = imageFileStream.ReadByte();

            var resolution = Resolution.LOW;

            if (width == 640)
            {
                if (height == 200) resolution = Resolution.MED;
                else if (height == 400) resolution = Resolution.HIGH;
            }

            return (resolution, width, height, bitPlanes);
        }

        internal override Color[] GetPalette(FileStream imageFileStream, int bitPlanes)
        {
            var paletteOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_COLORMAP);

            // Offset + chunk header + first 3 bytes of longword
            imageFileStream.Seek(paletteOffset + 4 + 3, SeekOrigin.Begin);

            // Number of colors is length of chunk div 3-component RGB
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

        internal override bool ImageHasAnimationData(FileStream imageFileStream, int bodyBytes)
        {
            var hasAnimations = false;
            var animOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_COLOR_RANGE);

            if (animOffset != -1)
            {
                imageFileStream.Seek(animOffset + 12, SeekOrigin.Begin);
                var animationEnabled = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
                if (animationEnabled == 1) hasAnimations = true;
            }

            return hasAnimations;
        }

        internal override Animation[] GetAnimations(FileStream imageFileStream, byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette)
        {
            var animations = new List<Animation>(0);
            var animOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_COLOR_RANGE);

            while (animOffset != -1)
            {
                imageFileStream.Seek(animOffset + 12, SeekOrigin.Begin);
                var animationEnabled = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();
                
                if (animationEnabled == 1)
                {
                    imageFileStream.Seek(-4, SeekOrigin.Current);
                    var animationSpeed = imageFileStream.ReadByte() << 8 | imageFileStream.ReadByte();

                    if (animationSpeed != 0)
                    {
                        var animationDelay = (float)1000/60 * (16384 / animationSpeed);

                        imageFileStream.Seek(2, SeekOrigin.Current);
                        var lowerLimit = imageFileStream.ReadByte();
                        var upperLimit = imageFileStream.ReadByte();

                        animations.Add(new Animation(imageBody, width, height, resolution, numBitPlanes, palette, lowerLimit, upperLimit, 0)
                        {
                            Delay = animationDelay,
                            Direction = AnimationDirection.Right
                        });
                    }

                    animOffset = GetChunkOffset(imageFileStream.Name, CHUNK_ID_COLOR_RANGE, (int)imageFileStream.Position);
                }
            }

            return animations.ToArray();
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
