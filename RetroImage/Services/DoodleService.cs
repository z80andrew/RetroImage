using System.IO;
using Z80andrew.RetroImage.Interfaces;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal class DoodleService : DegasService, IAtariImageService
    {
        protected override void Init()
        {
            PALETTE_OFFSET = 0x00;
            BODY_OFFSET = 0x00;
            MAX_ANIMATIONS = 0x00;
        }

        protected override (int width, int height, int bitPlanes) SetImageDimensions(Resolution resolution)
        {
            int width = 640;
            int height = 480;
            int bitPlanes = 1;

            return (width, height, bitPlanes);
        }

        protected override bool ImageHasAnimationData(FileStream imageFileStream, int bodyBytes)
        {
            return false;
        }

        protected override CompressionType GetCompressionType(byte compression)
        {
            return CompressionType.NONE;
        }

        protected override Resolution GetResolution(FileStream imageFileStream)
        {
            return Resolution.HIGH;
        }
    }
}
