using System.IO;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal class DoodleService : DegasService
    {
        internal override void Init()
        {
            PALETTE_OFFSET = 0x00;
            BODY_OFFSET = 0x00;
            MAX_ANIMATIONS = 0x00;
        }

        internal override (Resolution resolution, int width, int height, int bitPlanes) GetImageProperties(Stream imageStream)
        {
            var resolution = Resolution.HIGH;
            int width = 640;
            int height = 400;
            int bitPlanes = 1;

            return (resolution, width, height, bitPlanes);
        }

        internal override bool ImageHasAnimationData(Stream imageStream, int bodyBytes)
        {
            return false;
        }

        internal override CompressionType GetCompressionType(Stream imageStream)
        {
            return CompressionType.NONE;
        }
    }
}
