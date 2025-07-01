using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.JS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class VirtualTerminalBuffer : IVirtualTerminalBuffer
    {
        public IVirtualTerminal Terminal => _Terminal;
        public int Width => _Width;

        public VirtualTerminalBuffer(IJSObjectBinding jsBuffer, int width, IVirtualTerminal terminal)
        {
            _Binding = jsBuffer;
            _Width = width;
            _Terminal = terminal;
        }

        private IVirtualTerminal _Terminal;
        private int _Width;
        private IJSObjectBinding _Binding;

        public async Task<IBufferViewport> CreateViewport(int offsetX, int offsetY)
        {
            await using (var jsView = (IJSObject)await _Binding.CallPropertyAsync("createViewport", IJSValue.FromNumber(offsetX), IJSValue.FromNumber(offsetY)))
                return new BufferViewport(await jsView.BindAsync(), this);
        }

        public async Task LineFeedAsync(int count)
        {
            await _Binding.CallPropertyAsync("lineFeed", IJSValue.FromNumber(count));
        }

        public async Task WriteAsync(string text)
        {
            await _Binding.CallPropertyAsync("write", IJSValue.FromString(text));
        }

        public async Task<int> GetHeightAsync()
        {
            return (int)Math.Round(((IJSValue<double>)await _Binding.CallPropertyAsync("getBufferHeight")).Value);
        }

        public async Task<(int x, int y)> GetCursorPosAsync()
        {
            int x = (int)Math.Round(((IJSValue<double>)await _Binding.GetAsync("cursorX")).Value);
            int y = (int)Math.Round(((IJSValue<double>)await _Binding.GetAsync("cursorY")).Value);
            return (x, y);
        }

        public async Task SetCursorPosAsync(int x, int y)
        {
            await _Binding.SetAsync("cursorX", IJSValue.FromNumber(x));
            await _Binding.SetAsync("cursorY", IJSValue.FromNumber(y));
        }

        public async Task SetForegroundColorAsync(string color)
        {
            await _Binding.SetAsync("fg", IJSValue.FromString(color));
        }

        public async Task SetBackgroundColorAsync(string color)
        {
            await _Binding.SetAsync("bg", IJSValue.FromString(color));
        }

        public async ValueTask DisposeAsync()
        {
            await _Binding.DisposeAsync();
        }
    }
}
