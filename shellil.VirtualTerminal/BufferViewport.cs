using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.JS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class BufferViewport : IBufferViewport
    {
        public IVirtualTerminalBuffer SourceBuffer { get => _SourceBuffer; }

        public BufferViewport(IJSObjectBinding jsView, IVirtualTerminalBuffer sourceBuffer)
        {
            _Binding = jsView;
            _SourceBuffer = sourceBuffer;
        }

        private IJSObjectBinding _Binding;
        private IVirtualTerminalBuffer _SourceBuffer;

        public async Task PresentAsync()
        {
            await _Binding.CallPropertyAsync("present");
        }

        public async Task<(int x, int y)> GetScrollOffsetAsync()
        {
            var x = (int)Math.Round(((IJSValue<double>)await _Binding.GetAsync("viewportX")).Value);
            var y = (int)Math.Round(((IJSValue<double>)await _Binding.GetAsync("viewportX")).Value);
            return (x, y);
        }

        public async Task SetScrollOffsetAsync(int x, int y)
        {
            await _Binding.SetAsync("viewportX", IJSValue.FromNumber(x));
            await _Binding.SetAsync("viewportY", IJSValue.FromNumber(y));
        }

        public async Task ScrollAsync(int x, int y)
        {
            await _Binding.CallPropertyAsync("scroll", IJSValue.FromNumber(x), IJSValue.FromNumber(y));
        }

        public async Task ScrollCursorIntoViewAsync()
        {
            await _Binding.CallPropertyAsync("scrollCursorIntoView");
        }

        public async Task SetCursorStyleAsync(TerminalCursorState cursorState)
        {
            await _Binding.SetAsync("cursorState", IJSValue.FromString(cursorState.ToString().ToLower()));
        }

        public async ValueTask DisposeAsync()
        {
            await _Binding.DisposeAsync();
        }
    }
}
