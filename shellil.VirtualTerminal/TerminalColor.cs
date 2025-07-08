using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class TerminalColor
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public TerminalColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public TerminalColor(ushort rg, ushort ba)
        {
            R = (byte)(rg >> 8);
            G = (byte)(rg & 255);
            B = (byte)(ba >> 8);
            A = (byte)(ba & 255);
        }
    }

    public class TerminalPosition
    {
        public ushort X;
        public ushort Y;
        public ushort Columns => X;
        public ushort Rows => Y;

        public TerminalPosition(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }
    }
}
