using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor.UI
{
    public interface ITerminalUIComponent
    {
        public void Render(IFrameRenderContext context);
    }

    public interface IFrameRenderContext
    {
        public IFrameDriver Driver { get; }
        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public IBufferWriter FrameBuffer { get; }
        public void ClaimInputRegion(TerminalPosition position, TerminalPosition size, IFocusable handler);
    }

    public interface IFocusable : IVTInputHandler
    {
        public bool CanFocus { get; }
        public TerminalPosition ShownCursorPosition { get; }
        public TerminalCursorState ShownCursorState { get; }
        public void OnFocused();
        public void OnLostFocus();
    }
}
