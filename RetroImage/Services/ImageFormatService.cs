using System.Collections.Generic;
using System.IO;

namespace Z80andrew.RetroImage.Services
{
    internal class ImageFormatService
    {
        private static AtariImageService _neochromeService = new NEOchromeService();
        private static AtariImageService _degasService = new DegasService();
        private static AtariImageService _doodleService = new DoodleService();
        private static AtariImageService _iffService = new IFFService();
        private static AtariImageService _tinyService = new TinyService();

        public Dictionary<string, AtariImageService> fileExtensionServices = new()
        {
            {".NEO", _neochromeService},
            {".PI1", _degasService},
            {".PI2", _degasService},
            {".PI3", _degasService},
            {".PC1", _degasService},
            {".PC2", _degasService},
            {".PC3", _degasService},
            {".PIC", _degasService},
            {".DOO", _doodleService},
            {".IFF", _iffService},
            {".TNY", _tinyService},
            {".TN1", _tinyService},
            {".TN2", _tinyService},
            {".TN3", _tinyService}
            //{".TN4", _tinyService}
        };

        internal AtariImageService GetImageServiceForFilePath(string filePath)
        {
            AtariImageService imageService = null;

            var extension = Path.GetExtension(filePath).ToUpper();

            fileExtensionServices.TryGetValue(extension, out imageService);

            return imageService;
        }
    }
}
