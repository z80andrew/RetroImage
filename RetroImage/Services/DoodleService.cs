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

        internal override (Resolution resolution, int width, int height, int bitPlanes) GetImageProperties(FileStream imageFileStream)
        {
            var resolution = Resolution.HIGH;
            int width = 640;
            int height = 400;
            int bitPlanes = 1;

            return (resolution, width, height, bitPlanes);
        }

        internal override bool ImageHasAnimationData(FileStream imageFileStream, int bodyBytes)
        {
            return false;
        }

        internal override CompressionType GetCompressionType(FileStream imageFileStream)
        {
            return CompressionType.NONE;
        }
    }
}
