using Avalonia.Animation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using Z80andrew.RetroImage.Interfaces;
using Z80andrew.RetroImage.Services;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Models
{
    public class Animation
    {
        public AnimationDirection Direction { get; set; }
        public float Delay { get; set; }
        public int FrameIndex { get; set; }
        private int NumFrames { get; set; }
        public Image<Rgba32>[] Frames;

        public Animation(byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette, int lowerPaletteIndex, int upperPaletteIndex)
        {
            NumFrames = upperPaletteIndex - lowerPaletteIndex;
            Frames = GenerateFrames(imageBody, width, height, resolution, numBitPlanes, palette, lowerPaletteIndex, upperPaletteIndex);
        }

        private Image<Rgba32>[] GenerateFrames(byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette, int lowerPaletteIndex, int upperPaletteIndex)
        {
            var degasService = new DegasService();

            int numFrames = upperPaletteIndex - lowerPaletteIndex;

            var frames = new Image<Rgba32>[numFrames];

            var currentPalette = palette;
            
            for (int i = 0; i < numFrames; i++)
            {
                var newPalette = new Color[palette.Length];

                for (int c = 0; c < newPalette.Length; c++)
                {
                    newPalette[c] = currentPalette[c];
                }

                newPalette[lowerPaletteIndex] = currentPalette[upperPaletteIndex];

                for (int cIndex = lowerPaletteIndex; cIndex < upperPaletteIndex; cIndex++)
                {
                    newPalette[cIndex + 1] = currentPalette[cIndex];
                }

                frames[i] = degasService.GetImageFromRawData(
                    width,
                    height,
                    resolution,
                    numBitPlanes,
                    newPalette,
                    imageBody
                    );

                currentPalette = newPalette;
            }

            var animationFrames = frames;

            for (int i = 0; i < numFrames; i++)
            {
                int frameToCompare = i == (numFrames-1) ? 0 : i+1;

                for (int y = 0; y < frames[i].Height; y++)
                {
                    for (int x = 0; x < frames[i].Width; x++)
                    {
                        if (frames[i][x, y] == frames[frameToCompare][x, y]) animationFrames[i][x, y] = new Rgba32(0, 0, 0, 0);
                        else animationFrames[i][x, y] = frames[frameToCompare][x, y];
                    }
                }
            }

            return animationFrames;
        }

        internal void AdvanceFrame()
        {
            FrameIndex++;
            if(FrameIndex > NumFrames-1) FrameIndex = 0;
        }
    }
}
