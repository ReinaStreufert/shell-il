using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public interface IVirtualTerminal
    {
        public event Action<(int w, int h)>? OnResize;
        public event Action<char>? OnInputChar;
        public event Action<TerminalSpecialKey>? OnSpecialKey;
        public event Action<IVirtualTerminalContext> OnReady;
        public (int w, int h) WindowSize { get; }
    }

    public interface IVirtualTerminalContext
    {
        public Task<IVirtualTerminalBuffer> CreateBufferAsync(int cols);
    }

    public interface IVirtualTerminalBuffer : IAsyncDisposable
    {
        public IVirtualTerminal Terminal { get; }
        public int Width { get; }
        public Task<int> GetHeightAsync();
        public Task<(int x, int y)> GetCursorPosAsync();
        public Task SetCursorPosAsync(int x, int y);
        public Task SetForegroundColorAsync(string color);
        public Task SetBackgroundColorAsync(string color);
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

    public enum TerminalSpecialKey
    {
        Backspace,
        Enter,
        Tab,
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        ArrowUp
    };
}
