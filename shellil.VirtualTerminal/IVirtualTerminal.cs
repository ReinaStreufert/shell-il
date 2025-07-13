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
        public IVirtualTerminalService Service { get; }
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
        public Task<TerminalPosition> GetCursorPosAsync();
        public Task SetCursorPosAsync(int x, int y);
        public Task SetCursorPosAsync(TerminalPosition position);
        public Task SetForegroundColorAsync(TerminalColor color);
        public Task SetBackgroundColorAsync(TerminalColor color);
        public Task<TerminalColor> GetForegroundColorAsync();
        public Task<TerminalColor> GetBackgroundColorAsync();
        public Task<IBufferViewport> CreateViewportAsync(int offsetX, int offsetY);
        public Task<IBufferViewport> CreateViewportAsync(TerminalPosition scrollOffset);
        public Task WriteAsync(string text);
        public Task LineFeedAsync(int count);
        public Task FlushAsync();
    }

    public interface IBufferViewport : IAsyncDisposable
    {
        public IVirtualTerminalBuffer SourceBuffer { get; }
        public Task<TerminalPosition> GetScrollOffsetAsync();
        public Task ScrollToAsync(int x, int y);
        public Task ScrollToAsync(TerminalPosition scrollOffset);
        public Task ScrollAsync(int x, int y);
        public Task ScrollAsync(TerminalPosition offset);
        public Task SetCursorStateAsync(TerminalCursorState cursorState);
        public Task ScrollCursorIntoViewAsync();
        public Task PresentAsync();
        public Task FlushAsync();
    }

    public enum TerminalCursorState
    {
        Invisible,
        Blink,
        Solid
    }

    
}
