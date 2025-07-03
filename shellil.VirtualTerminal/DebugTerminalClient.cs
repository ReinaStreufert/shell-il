using LibChromeDotNet.HTML5.DOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class DebugTerminalClient : IVirtualTerminalClient
    {
        public DebugTerminalClient(string filePath)
        {
            _InitialBufferText = File.ReadAllLines(filePath);
        }

        private IVirtualTerminalBuffer? _Buffer;
        private IBufferViewport? _View;
        private string[] _InitialBufferText;

        public async Task OnReadyAsync(IVirtualTerminalContext ctx)
        {
            var buffer = await ctx.CreateBufferAsync(200);
            for (int y = 0; y < _InitialBufferText.Length; y++)
                await buffer.WriteLineAsync(_InitialBufferText[y]);
            await buffer.SetCursorPosAsync(0, 0);
            var view = await buffer.CreateViewport(0, 0);
            _Buffer = buffer;
            _View = view;

            var appWindow = ctx.GetAppWindow();
            var docBody = await appWindow.GetDocumentBodyAsync();
            var bgColorInput = await HTMLInputElement.FromDOMNodeAsync(await docBody.QuerySelectAsync("#bginput"));
            var fgColorInput = await HTMLInputElement.FromDOMNodeAsync(await docBody.QuerySelectAsync("#fginput"));
            bgColorInput.ValueChanged += async () => await buffer.SetBackgroundColorAsync(bgColorInput.Value);
            fgColorInput.ValueChanged += async () => await buffer.SetForegroundColorAsync(fgColorInput.Value);
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
                if (cursorPos.x > 0)
                {
                    await _Buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
                    await _Buffer.WriteAsync(" ");
                    await _Buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
                }
                else if (cursorPos.y > 0)
                {
                    await _Buffer.SetCursorPosAsync(0, cursorPos.y - 1);
                    await _Buffer.WriteAsync(new string(' ', _Buffer.Width));
                    await _Buffer.SetCursorPosAsync(0, cursorPos.y - 1);
                }
            }
            if (key == TerminalSpecialKey.ArrowLeft && cursorPos.x > 0)
                await _Buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
            else if (key == TerminalSpecialKey.ArrowRight && cursorPos.x + 1 < _Buffer.Width)
                await _Buffer.SetCursorPosAsync(cursorPos.x + 1, cursorPos.y);
            else if (key == TerminalSpecialKey.ArrowUp && cursorPos.y > 0)
                await _Buffer.SetCursorPosAsync(cursorPos.x, cursorPos.y - 1);
            else if (key == TerminalSpecialKey.ArrowDown && cursorPos.y + 1 < bufferHeight)
                await _Buffer.SetCursorPosAsync(cursorPos.x, cursorPos.y + 1);
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

        public Task OnViewportResizeAsync(int widthCols, int heightRows)
        {
            return Task.CompletedTask;
        }

        public Task OnMouseEventAsync(TerminalMouseEventType type, int x, int y)
        {
            return Task.CompletedTask;
        }
    }
}
