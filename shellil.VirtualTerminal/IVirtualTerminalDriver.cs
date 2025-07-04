using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public interface IVirtualTerminalDriver
    {
        public Task OnReadyAsync(IVirtualTerminalContext ctx);
        public Task OnViewportResizeAsync(int widthCols, int heightRows);
        public Task OnInputCharAsync(char inputChar, TerminalModifierKeys modifiers);
        public Task OnSpecialKeyAsync(TerminalSpecialKey key, TerminalModifierKeys modifiers);
        public Task OnUserScrollAsync(int deltaX, int deltaY);
        public Task OnMouseEventAsync(TerminalMouseEventType type, int x, int y);
    }

    public enum TerminalSpecialKey
    {
        Backspace = 0,
        Enter = 1,
        Tab = 2,
        ArrowUp = 3,
        ArrowDown = 4,
        ArrowLeft = 5,
        ArrowRight = 6
    };

    [Flags]
    public enum TerminalModifierKeys
    {
        None = 0,
        Shift = 1,
        Alt = 2,
        Meta = 4,
        Ctrl = 8
    }

    public enum TerminalMouseEventType
    {
        MouseMove = 0,
        MouseDown = 1,
        MouseUp = 2
    }
}
