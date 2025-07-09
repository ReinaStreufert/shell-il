using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public static class VTProtocol
    {
        public const ushort HB_FRONTENDREADY = 0x00;
        public const ushort HB_VIEWRESIZE = 0x01;
        public const ushort HB_INPUTCHAR = 0x02;
        public const ushort HB_SPECIALKEY = 0x03;
        public const ushort HB_USERSCROLL = 0x04;
        public const ushort HB_MOUSE = 0x05;
        public const ushort HB_BUFFERCREATED = 0x06;
        public const ushort HB_VIEWPORTCREATED = 0x07;
        public const ushort HB_BUFFERUPDATED = 0x08;
        public const ushort HB_VIEWPORTUPDATED = 0x09;
        public const ushort HB_REQUESTPROCESSED = 0x0A;
        public const ushort CB_CREATEBUFFER = 0x00;
        public const ushort CB_CREATEVIEWPORT = 0x01;
        public const ushort CB_WRITEBUFFER = 0x02;
        public const ushort CB_SETBUFFERATTR = 0x03;
        public const ushort CB_VIEWPORTCOMMAND = 0x04;

        [Flags]
        public enum BufferUpdateFlags
        {
            None = 0,
            BufferSize = 1,
            CursorPos = 2,
            BackgroundColor = 4,
            ForegroundColor = 8
        }

        [Flags]
        public enum BufferModifyFlags
        {
            None = 0,
            SetCursorPos = 1,
            SetBackgroundColor = 2,
            SetForegroundColor = 4,
            ApplyLineFeed = 8
        }
    }
}
