using LibChromeDotNet;
using LibChromeDotNet.HTML5.DOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    // all of this stuff is just for testing both the frontend and backend of the library and will all be deleted later.
    public class Program
    {
        public static async Task Main(string[] args)
        {
            
            var terminal = new VirtualTerminal();
            terminal.OnReady += (ctx) => _ = OnReady(terminal, ctx);
            await terminal.LaunchAsync();
        }

        private static async Task OnReady(IVirtualTerminal terminal, IVirtualTerminalContext ctx)
        {
            var buffer = await ctx.CreateBufferAsync(200);
            var view = await buffer.CreateViewport(0, 0);

            var appWindow = ctx.GetAppWindow();
            var docBody = await appWindow.GetDocumentBodyAsync();
            var bgColorInput = await HTMLInputElement.FromDOMNodeAsync(await docBody.QuerySelectAsync("#bginput"));
            var fgColorInput = await HTMLInputElement.FromDOMNodeAsync(await docBody.QuerySelectAsync("#fginput"));
            bgColorInput.ValueChanged += async () => await buffer.SetBackgroundColorAsync(bgColorInput.Value);
            fgColorInput.ValueChanged += async () => await buffer.SetForegroundColorAsync(fgColorInput.Value);

            terminal.OnInputChar += async c =>
            {
                await buffer.WriteAsync(c.ToString());
                await view.ScrollCursorIntoViewAsync();
                await view.PresentAsync();
            };
            terminal.OnSpecialKey += async key =>
            {
                var cursorPos = await buffer.GetCursorPosAsync();
                var bufferHeight = await buffer.GetHeightAsync();
                if (key == TerminalSpecialKey.Enter)
                {
                    await buffer.WriteLineAsync();
                }else if (key == TerminalSpecialKey.Backspace)
                {
                    if (cursorPos.x > 0)
                    {
                        await buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
                        await buffer.WriteAsync(" ");
                        await buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
                    }
                    else if (cursorPos.y > 0)
                    {
                        await buffer.SetCursorPosAsync(0, cursorPos.y - 1);
                        await buffer.WriteAsync(new string(' ', buffer.Width));
                        await buffer.SetCursorPosAsync(0, cursorPos.y - 1);
                    }
                }
                if (key == TerminalSpecialKey.ArrowLeft && cursorPos.x > 0)
                    await buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
                else if (key == TerminalSpecialKey.ArrowRight && cursorPos.x + 1 < buffer.Width)
                    await buffer.SetCursorPosAsync(cursorPos.x + 1, cursorPos.y);
                else if (key == TerminalSpecialKey.ArrowUp && cursorPos.y > 0)
                    await buffer.SetCursorPosAsync(cursorPos.x, cursorPos.y - 1);
                else if (key == TerminalSpecialKey.ArrowDown && cursorPos.y + 1 < bufferHeight)
                    await buffer.SetCursorPosAsync(cursorPos.x, cursorPos.y + 1);
                await view.ScrollCursorIntoViewAsync();
                await view.PresentAsync();
            };
        }
    }
}
