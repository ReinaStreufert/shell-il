using LibChromeDotNet.HTML5.DOM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class DebugTerminalDriver : IVirtualTerminalDriver
    {
        public DebugTerminalDriver(string filePath)
        {
            _InitialBufferText = File.ReadAllLines(filePath);
        }

        private IVirtualTerminalBuffer? _Buffer;
        private IBufferViewport? _View;
        private string[] _InitialBufferText;

        public async Task OnReadyAsync(IVirtualTerminalContext ctx)
        {
            Debug.WriteLine("ready");
            var bufferWidth = _InitialBufferText.Select(l => l.Length).Max();
            var buffer = await ctx.CreateBufferAsync(bufferWidth);
            await buffer.LineFeedAsync(_InitialBufferText.Length - 1);
            for (int y = 0; y < _InitialBufferText.Length; y++)
            {
                await buffer.SetCursorPosAsync(0, y);
                await buffer.WriteAsync(_InitialBufferText[y]);
            }
            await buffer.SetCursorPosAsync(0, 0);
            var view = await buffer.CreateViewportAsync(0, 0);
            _Buffer = buffer;
            _View = view;
            await _View.PresentAsync();
        }

        public async Task OnInputCharAsync(char inputChar, TerminalModifierKeys modifiers)
        {
            if (_Buffer == null || _View == null)
                return;
            await _Buffer.WriteAsync(inputChar.ToString());
            await _View.ScrollCursorIntoViewAsync();
            await _View.PresentAsync();
        }

        public async Task OnSpecialKeyAsync(TerminalSpecialKey key, TerminalModifierKeys modifiers)
        {
            if (_Buffer == null || _View == null)
                return;
            var cursorPos = await _Buffer.GetCursorPosAsync();
            var bufferHeight = await _Buffer.GetHeightAsync();
            if (key == TerminalSpecialKey.Enter)
            {
                await _Buffer.WriteLineAsync();
            }
            else if (key == TerminalSpecialKey.Backspace)
            {
                if (cursorPos.X > 0)
                {
                    await _Buffer.SetCursorPosAsync(cursorPos.X - 1, cursorPos.Y);
                    await _Buffer.WriteAsync(" ");
                    await _Buffer.SetCursorPosAsync(cursorPos.X - 1, cursorPos.Y);
                }
                else if (cursorPos.Y > 0)
                {
                    await _Buffer.SetCursorPosAsync(0, cursorPos.Y - 1);
                    await _Buffer.WriteAsync(new string(' ', _Buffer.Width));
                    await _Buffer.SetCursorPosAsync(0, cursorPos.Y - 1);
                }
            }
            if (key == TerminalSpecialKey.ArrowLeft && cursorPos.X > 0)
                await _Buffer.SetCursorPosAsync(cursorPos.X - 1, cursorPos.Y);
            else if (key == TerminalSpecialKey.ArrowRight && cursorPos.X + 1 < _Buffer.Width)
                await _Buffer.SetCursorPosAsync(cursorPos.X + 1, cursorPos.Y);
            else if (key == TerminalSpecialKey.ArrowUp && cursorPos.Y > 0)
                await _Buffer.SetCursorPosAsync(cursorPos.X, cursorPos.Y - 1);
            else if (key == TerminalSpecialKey.ArrowDown && cursorPos.Y + 1 < bufferHeight)
                await _Buffer.SetCursorPosAsync(cursorPos.X, cursorPos.Y + 1);
            await _View.ScrollCursorIntoViewAsync();
            await _View.PresentAsync();
        }

        public async Task OnUserScrollAsync(int deltaX, int deltaY)
        {
            if (_Buffer == null || _View == null)
                return;
            await _View.ScrollAsync(deltaX, deltaY);
            await _View.PresentAsync();
        }

        public async Task OnViewportResizeAsync(int widthCols, int heightRows)
        {
            if (_Buffer == null || _View == null)
                return;
            await _View.PresentAsync();
        }

        public async Task OnMouseEventAsync(TerminalMouseEventType type, int x, int y)
        {
            if (_Buffer == null || _View == null)
                return;
            if (type == TerminalMouseEventType.MouseDown)
            {
                var scrollOffset = await _View.GetScrollOffsetAsync();
                await _Buffer.SetCursorPosAsync(scrollOffset.X + x, scrollOffset.Y + y);
                await _View.PresentAsync();
            }
        }
    }
}
