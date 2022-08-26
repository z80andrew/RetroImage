using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z80andrew.RetroImage.Common
{
    public static class Constants
    {
        public static int SCREEN_MEMORY_BYTES = 32000;

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
    }
}
