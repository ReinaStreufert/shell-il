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
        public int InitialViewportWidth { get; }
        public int InitialViewportHeight { get; }
        public Task<IVirtualTerminalBuffer> CreateBufferAsync(int cols);
    }

    public interface IVirtualTerminalBuffer : IAsyncDisposable
    {
        public int Width { get; }
        public Task<int> GetHeightAsync();
        public Task<(int x, int y)> GetCursorPosAsync();
        public Task SetCursorPosAsync(int x, int y);
        public Task SetForegroundColorAsync(TerminalColor color);
        public Task SetBackgroundColorAsync(TerminalColor color);
        public Task<TerminalColor> GetForegroundColorAsync();
        public Task<TerminalColor> GetBackgroundColorAsync();
        public Task<IBufferViewport> CreateViewportAsync(int offsetX, int offsetY);
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
