using Z80andrew.RetroImage.Interfaces;

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
