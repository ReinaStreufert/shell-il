using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public static class VirtualTerminalExtensions
    {
        public static async Task WriteLineAsync(this IVirtualTerminalBuffer buffer)
        {
            var height = await buffer.GetHeightAsync();
            var cursorPos = await buffer.GetCursorPosAsync();
            if (cursorPos.Y + 1 >= height)
                await buffer.LineFeedAsync(1);
            if (cursorPos.X < buffer.Width)
                await buffer.WriteAsync(new string(' ', buffer.Width - cursorPos.X));
            await buffer.SetCursorPosAsync(0, cursorPos.Y + 1);
        }

        public static async Task WriteLineAsync(this IVirtualTerminalBuffer buffer, string text)
        {
            await buffer.WriteAsync(text);
            await buffer.WriteLineAsync();
        }
    }
}
