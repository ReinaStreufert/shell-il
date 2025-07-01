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
        public (int w, int h) WindowSize { get; }
        public IVirtualTerminalBuffer CreateBuffer(int cols);
    }

    public interface IVirtualTerminalBuffer
    {
        public IVirtualTerminal Terminal { get; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int CursorX { get; set; }
        public int CursorY { get; set; }
        public string ForegroundColor { get; set; }
        public string BackgroundColor { get; set; }
        public Task<IBufferViewport> CreateViewport(int offsetX, int offsetY);
        public Task WriteAsync(string text);
        public Task WriteLineAsync();
        public Task LineFeedAsync();
    }

    public interface IBufferViewport
    {
        public IVirtualTerminalBuffer SourceBuffer { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public TerminalCursorState CursorState { get; set; }
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
