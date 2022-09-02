using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Z80andrew.RetroImage.Services;
using static Z80andrew.RetroImage.Common.Constants;

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

            var animationFrames = frames;

            for (int i = 0; i < numFrames; i++)
            {
                for (int y = 0; y < frames[i].Height; y++)
                {
                    for (int x = 0; x < frames[i].Width; x++)
                    {
                        if (frames[i][x, y] == frames[0][x, y]) animationFrames[i][x, y] = RGBA_TRANSPARENT;
                        else animationFrames[i][x, y] = frames[i][x, y];
                    }
                }
            }

            return animationFrames;
        }

        internal void AdvanceFrame()
        {
            FrameIndex++;
            if (FrameIndex > NumFrames) FrameIndex = 0;
        }
    }
}
