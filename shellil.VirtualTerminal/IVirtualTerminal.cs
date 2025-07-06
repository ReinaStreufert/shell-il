using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public interface IVirtualTerminalHost
    {
        public ITerminalDriverFactory DriverFactory { get; }
        public Task ListenAsync(IEnumerable<string> httpPrefixes, CancellationToken cancelToken);
    }

    public interface IVirtualTerminalContext
    {
        public Task<IVirtualTerminalBuffer> CreateBufferAsync(int cols);
    }

    public interface IVirtualTerminalBuffer : IAsyncDisposable
    {
        public int Width { get; }
        public Task<int> GetHeightAsync();
        public Task<(int x, int y)> GetCursorPosAsync();
        public Task SetCursorPosAsync(int x, int y);
        public Task SetForegroundColorAsync(byte r, byte g, byte b, byte a);
        public Task SetBackgroundColorAsync(byte r, byte g, byte b, byte a);
        public Task<(byte r, byte g, byte b, byte a)> GetForegroundColorAsync();
        public Task<(byte r, byte g, byte b, byte a)> GetBackgroundColorAsync();
        public Task<IBufferViewport> CreateViewport(int offsetX, int offsetY);
        public Task WriteAsync(string text);
        public Task LineFeedAsync(int count);
    }

    public interface IBufferViewport : IAsyncDisposable
    {
        public IVirtualTerminalBuffer SourceBuffer { get; }
        public Task<(int x, int y)> GetScrollOffsetAsync();
        public Task SetScrollOffsetAsync(int x, int y);
        public Task ScrollAsync(int x, int y);
        public Task SetCursorStyleAsync(TerminalCursorState cursorState);
        public Task ScrollCursorIntoViewAsync();
        public Task PresentAsync();
    }

    public enum TerminalCursorState
    {
        Invisible,
        Blink,
        Solid
    }

    
}
