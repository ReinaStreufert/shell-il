using LibChromeDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
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
            terminal.OnInputChar += async c =>
            {
                await buffer.WriteAsync(c.ToString());
                await view.ScrollCursorIntoViewAsync();
                await view.PresentAsync();
            };
            terminal.OnSpecialKey += async key =>
            {
                if (key == TerminalSpecialKey.Backspace)
                {
                    var cursorPos = await buffer.GetCursorPosAsync();
                    await buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
                    await buffer.WriteAsync(" ");
                    await buffer.SetCursorPosAsync(cursorPos.x - 1, cursorPos.y);
                }
                await view.ScrollCursorIntoViewAsync();
                await view.PresentAsync();
            };
        }
    }
}
