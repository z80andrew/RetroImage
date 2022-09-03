using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using Z80andrew.RetroImage.Services;
using static Z80andrew.RetroImage.Common.Constants;
using Color = SixLabors.ImageSharp.Color;

namespace Z80andrew.RetroImage.Models
{
    public class Animation
    {
        public int AnimationLayer { get; set; }
        public AnimationDirection Direction { get; set; }
        public float Delay { get; set; }
        public int FrameIndex { get; set; }
        private int NumFrames { get; set; }
        public Image<Rgba32>[] Frames;

        internal Animation(byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette, int lowerPaletteIndex, int upperPaletteIndex, int animationLayer)
        {
            NumFrames = upperPaletteIndex - lowerPaletteIndex;
            Frames = GenerateFrames(imageBody, width, height, resolution, numBitPlanes, palette, lowerPaletteIndex, upperPaletteIndex);
            AnimationLayer = animationLayer;
        }

        internal Image<Rgba32>[] GenerateFrames(byte[] imageBody, int width, int height, Resolution resolution, int numBitPlanes, Color[] palette, int lowerPaletteIndex, int upperPaletteIndex)
        {
            var degasService = new DegasService();

            int numFrames = (upperPaletteIndex - lowerPaletteIndex) + 1;

            var frames = new Image<Rgba32>[numFrames];

            frames[0] = degasService.GetImageFromRawData(
                width,
                height,
                resolution,
                numBitPlanes,
                palette,
                imageBody
                );

            var currentPalette = palette;

            for (int i = 1; i < numFrames; i++)
            {
                var newPalette = new Color[palette.Length];

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

            GenerateAnimatedGif(frames);

            // Remove duplicate color data in animation frames
            for (int i = 1; i < numFrames; i++)
            {
                for (int y = 0; y < frames[i].Height; y++)
                {
                    for (int x = 0; x < frames[i].Width; x++)
                    {
                        if (frames[i][x, y] == frames[0][x, y]) frames[i][x, y] = RGBA_TRANSPARENT;
                    }
                }
            }

            return frames;
        }

        internal void GenerateAnimatedGif(Image<Rgba32>[] frames)
        {
            var gif = frames[0];

            var gifMetaData = gif.Metadata.GetGifMetadata();
            gifMetaData.RepeatCount = 0;
            gifMetaData.ColorTableMode = GifColorTableMode.Global;
            gifMetaData.Comments = new List<string>() { "Made with RetroImage by z80andrew" };

            GifFrameMetadata metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();

            for (int i = 1; i < frames.Length; i++)
            {
                metadata = frames[i].Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = Convert.ToInt32(Delay);

                gif.Frames.AddFrame(frames[i].Frames.RootFrame);
            }

            gif.SaveAsGif(@"d:\temp\ataripics\output.gif");
        }

        internal void AdvanceFrame()
        {
            FrameIndex++;
            if (FrameIndex > NumFrames) FrameIndex = 0;
        }
    }
}
