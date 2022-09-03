﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Models
{
    public class AtariImageModel
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public CompressionType Compression { get; set; }
        public int NumBitPlanes { get; set; }
        public Color[] Palette { get; set; }
        public Image<Rgba32> Image { get; set; }
        public Resolution Resolution { get; set; }
        public byte[] RawData { get; set; }
        public Animation[] Animations { get; set; }
    }
}