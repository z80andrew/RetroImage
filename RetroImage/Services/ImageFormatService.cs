using System.IO;

namespace Z80andrew.RetroImage.Services
{
    internal class ImageFormatService
    {
        internal AtariImageService GetImageServiceForFileExtension(string filePath)
        {
            AtariImageService imageService = null;

            switch (Path.GetExtension(filePath.ToUpper()))
            {
                case ".NEO":
                    imageService = new NEOchromeService();
                    break;
                case ".PI1":
                case ".PI2":
                case ".PI3":
                case ".PC1":
                case ".PC2":
                case ".PC3":
                case ".PIC":
                    imageService = new DegasService();
                    break;
                case ".DOO":
                    imageService = new DoodleService();
                    break;
                case ".IFF":
                    imageService = new IFFService();
                    break;
                case ".TNY":
                case ".TN1":
                case ".TN2":
                case ".TN3":
                    imageService = new TinyService();
                    break;
            }

            return imageService;
        }
    }
}
