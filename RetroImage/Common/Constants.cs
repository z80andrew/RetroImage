using SixLabors.ImageSharp.PixelFormats;

namespace Z80andrew.RetroImage.Common
{
    public static class Constants
    {
        public static int SCREEN_MEMORY_BYTES = 32000;

        public static Rgba32 RGBA_TRANSPARENT => new Rgba32(0, 0, 0, 0);

        public enum AnimationDirection
        {
            Left = 0,
            None,
            Right,
        }

        public enum Resolution
        {
            LOW = 0,
            MED,
            HIGH,
        }

        public enum CompressionType
        {
            NONE = 0,
            PACKBITS,
            VERTICAL_RLE1,
            VERTICAL_RLE2,
        }
    }
}
