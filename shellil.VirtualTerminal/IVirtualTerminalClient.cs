using LibChromeDotNet.HTML5.DOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public interface IVirtualTerminalClient
    {
        public Task OnReadyAsync(IVirtualTerminalContext ctx);
        public Task OnViewportResizeAsync(int widthCols, int heightRows);
        public Task OnInputCharAsync(char inputChar, ModifierKeys modifiers);
        public Task OnSpecialKeyAsync(TerminalSpecialKey key, ModifierKeys modifiers);
        public Task OnUserScrollAsync(int deltaX, int deltaY);
        public Task OnMouseEventAsync(TerminalMouseEventType type, int x, int y);
    }

    public enum TerminalSpecialKey
    {
        Backspace,
        Enter,
        Tab,
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        ArrowUp
    };

    public enum TerminalMouseEventType
    {
        MouseMove,
        MouseDown,
        MouseUp
    }
}
