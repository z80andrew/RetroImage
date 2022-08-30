using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z80andrew.RetroImage.Models
{
    internal class VerticalRleModel
    {
        internal byte[] CommandBytes { get; set; }
        internal byte[] DataBytes { get; set; }
    }
}
