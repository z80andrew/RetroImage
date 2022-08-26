using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Interfaces
{
    public interface IAtariImageService
    {
        IAtariImage GetImage(string path);
        Image<Rgba32> GetImageFromRawData(int width, int height, Resolution resolution, int bitPlanes,  Color[] colors, byte[] imageBytes);

    }
}
