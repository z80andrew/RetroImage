using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Models
{
    public class AtariImageModel
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int RenderHeight { get; set; }
        public CompressionType Compression { get; set; }
        public int NumBitPlanes { get; set; }
        public Color[] Palette { get; set; }
        public Image<Rgba32> Image { get; set; }
        public Resolution Resolution { get; set; }
        public byte[] RawData { get; set; }
        public Animation[] Animations { get; set; }

        internal async Task ExportImageToFile(string exportPath)
        {
            if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);

            if (Image != null)
            {
                if (Animations.Length > 0)
                    await ExportAnimationToFile(exportPath);
                else
                    await ExportStaticImageToFile(exportPath);
            }
        }

        private async Task ExportStaticImageToFile(string exportPath)
        {
            using (var fileStream = new FileStream(Path.Combine(exportPath, Name + ".png"), FileMode.Create))
            {
                var encoder = new PngEncoder();
                encoder.BitDepth = PngBitDepth.Bit4;
                encoder.ColorType = PngColorType.Palette;
                encoder.CompressionLevel = PngCompressionLevel.BestCompression;
                await Image.SaveAsPngAsync(fileStream, encoder);
            }
        }

        internal async Task ExportAnimationToFile(string exportPath)
        {
            if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);

            var gif = Animations[0].Frames[0];

            var gifMetaData = gif.Metadata.GetGifMetadata();
            gifMetaData.RepeatCount = 0;
            gifMetaData.ColorTableMode = GifColorTableMode.Global;
            gifMetaData.Comments = new List<string>() { "Converted from Atari format by RetroImage" };

            GifFrameMetadata metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
            metadata.FrameDelay = Convert.ToInt32(Animations[0].Delay / 10);

            for (int i = 1; i < Animations[0].Frames.Length; i++)
            {
                metadata = Animations[0].Frames[i].Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = Convert.ToInt32(Animations[0].Delay / 10);

                gif.Frames.AddFrame(Animations[0].Frames[i].Frames.RootFrame);
            }

            await gif.SaveAsGifAsync(Path.Combine(exportPath, Name + ".gif"));
        }
    }
}
