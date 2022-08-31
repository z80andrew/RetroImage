using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Interfaces
{
    public interface IAtariImage
    {
        int Width { get; set; }
        int Height { get; set; }
        Resolution Resolution { get; set; }
        CompressionType Compression { get; set; }
        int NumBitPlanes { set; get; }
        Color[] Palette { get; set; }
        byte[] RawData { get; set; }
        Image<Rgba32> Image { get; set; }
        Animation[] Animations { get; set; }
    }
}
