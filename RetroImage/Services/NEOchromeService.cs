using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using Z80andrew.RetroImage.Interfaces;
using static Z80andrew.RetroImage.Common.Constants;
using Z80andrew.RetroImage.Models;

namespace Z80andrew.RetroImage.Services
{
    internal class NEOchromeService : DegasService, IAtariImageService
    {
        protected override void Init()
        {
            PALETTE_OFFSET = 0x04;
            BODY_OFFSET = 0x80;
            MAX_ANIMATIONS = 0x04;
        }
    }
}
