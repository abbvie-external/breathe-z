using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cf.Tecc.Controller
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ActiveHardware
    {
        public int Init;
        public int Handle;
        public byte DeviceCount;
        public unsafe fixed byte Data[256];
        public int Serial;
    }
}
