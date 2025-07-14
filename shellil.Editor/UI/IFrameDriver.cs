using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor.UI
{
    public interface IFrameDriver : IVirtualTerminalDriver
    {
        public void Invalidate();
        public TerminalPosition ViewportSize { get; }
    }
}
