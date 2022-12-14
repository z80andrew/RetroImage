using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Z80andrew.RetroImage.Common;
using Z80andrew.RetroImage.Models;
using static Z80andrew.RetroImage.Common.Constants;

namespace Z80andrew.RetroImage.Services
{
    internal class NEOchromeService : DegasService
    {
        private int ANIMATION_OFFSET = 0x30;
        internal override void Init()
        {
            RESOLUTION_OFFSET = 0x03;
            PALETTE_OFFSET = 0x04;
            BODY_OFFSET = 0x80;
            MAX_ANIMATIONS = 0x04;
        }

        internal override bool ImageHasAnimationData(Stream imageStream, int bodyBytes)
        {
            bool imageHasAnimationData = false;

            imageStream.Seek(ANIMATION_OFFSET, SeekOrigin.Begin);

            var animationValidByte = imageStream.ReadByte();

            if ((animationValidByte & 0x80) == 0x80)
            {
                imageHasAnimationData = true;
            }

            return imageHasAnimationData;
        }

        internal override Animation[] GetAnimations(Stream imageStream, byte[] imageBody, int width, int height, int renderHeight, Constants.Resolution resolution, int numBitPlanes, Color[] palette)
        {
            var animations = new List<Animation>();

            imageStream.Seek(ANIMATION_OFFSET + 1, SeekOrigin.Begin);

            var animationLimitsByte = Convert.ToByte(imageStream.ReadByte());

            int upperAnimationLimit = (animationLimitsByte >> 4) & 0x0F;
            int lowerAnimationLimit = animationLimitsByte & 0x0F;

            var animationDescriptionByte = Convert.ToByte(imageStream.ReadByte());

            var animationEnabled = (animationDescriptionByte & 0x80) == 0x80;

            var vBlanks = (sbyte)imageStream.ReadByte();

            var animationDirection = vBlanks < 0 ? AnimationDirection.Left : AnimationDirection.Right;

            if (vBlanks < 0) vBlanks *= -1;

            var animationDelay = (1000 / 60) * (vBlanks - 1);

            if (animationEnabled)
            {

                animations.Add(new Animation(imageBody, width, height, renderHeight, resolution, numBitPlanes, palette, upperAnimationLimit, lowerAnimationLimit, 0)
                {
                    Delay = animationDelay,
                    Direction = animationDirection
                });
            }

            return animations.ToArray();
        }
    }
}
