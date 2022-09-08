using Avalonia;

namespace Z80andrew.RetroImage.Exntensions
{
    internal static class Extensions
    {
        internal static PixelRect Mutiply(this PixelRect pr1, float i)
        {
            return new PixelRect((int)(pr1.X * i), (int)(pr1.Y * i), (int)(pr1.Width * i), (int)(pr1.Height * i));
        }
    }
}
