using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor.UI
{
    public class EditorView : ITerminalUIComponent
    {
        public EditorView(TextEditor editor, int topMargin)
        {
            _Editor = editor;
            _TopMargin = topMargin;
        }

        private TextEditor _Editor;
        private int _TopMargin;

        public void Render(IFrameRenderContext context)
        {
            var renderHeight = context.FrameHeight - _TopMargin;
            var activeTab = _Editor.ActiveTab;
            var activeBuffer = activeTab.Buffer;
            var frameBuffer = context.FrameBuffer;
            frameBuffer.SetBackgroundColor(_Editor.Appearance.BufferPlainTextBackground);
            frameBuffer.SetForegroundColor(_Editor.Appearance.BufferPlainTextForeground);
            for (int clientY = 0; clientY < renderHeight; clientY++)
            {
                var row = activeTab.ScrollOffset.Y + clientY;
                var lineLength = activeBuffer.GetLineLength(row);
                var lineRangeStart = activeTab.ScrollOffset.X;
                var lineRangeLength = Math.Min(context.FrameWidth, lineLength - lineRangeStart);
                if (lineRangeLength == 0)
                    continue;
                frameBuffer.SetCursorPosition(new TerminalPosition(0, clientY + _TopMargin));
                frameBuffer.Write(activeBuffer.GetTextRange(row, lineRangeStart, lineRangeLength));
            }
            var inputHandler = new InputHandler(this, context.Driver);
            context.ClaimInputRegion(new TerminalPosition(0, _TopMargin), new TerminalPosition(context.FrameWidth, renderHeight), inputHandler);
        }

        private class InputHandler : IFocusable
        {
            public bool CanFocus => true;
            public TerminalPosition ShownCursorPosition
            {
                get
                {
                    var activeTab = _EditorView._Editor.ActiveTab;
                    var x = activeTab.CursorPosition.X - activeTab.ScrollOffset.X;
                    var y = activeTab.CursorPosition.Y - activeTab.ScrollOffset.Y + _EditorView._TopMargin;
                    return new TerminalPosition(x, y);
                }
            }
            public TerminalCursorState ShownCursorState => TerminalCursorState.Blink;

            public InputHandler(EditorView editorView, IFrameDriver driver)
            {
                _EditorView = editorView;
                _Driver = driver;
            }

            private EditorView _EditorView;
            private IFrameDriver _Driver;

            public void OnFocused()
            {
                return;
            }

            public void OnLostFocus()
            {
                return;
            }

            public Task OnInputCharAsync(char inputChar, TerminalModifierKeys modifiers)
            {
                if (modifiers == TerminalModifierKeys.None)
                {
                    var activeTab = _EditorView._Editor.ActiveTab;
                    activeTab.Write(inputChar);
                    var viewSize = _Driver.ViewportSize;
                    activeTab.ScrollCursorInBounds(viewSize.X, viewSize.Y - _EditorView._TopMargin);
                    _Driver.Invalidate();
                }
                return Task.CompletedTask;
            }

            public Task OnSpecialKeyAsync(TerminalSpecialKey key, TerminalModifierKeys modifiers)
            {
                var activeTab = _EditorView._Editor.ActiveTab;
                if (key == TerminalSpecialKey.Enter)
                    activeTab.WriteLineBreak();
                else if (key == TerminalSpecialKey.Backspace)
                    activeTab.WriteBackspace();
                else if (key == TerminalSpecialKey.ArrowLeft)
                    activeTab.SeekHorizontal(-1);
                else if (key == TerminalSpecialKey.ArrowRight)
                    activeTab.SeekHorizontal(1);
                else if (key == TerminalSpecialKey.ArrowUp)
                    activeTab.SeekVertical(-1);
                else if (key == TerminalSpecialKey.ArrowDown)
                    activeTab.SeekVertical(1);
                else
                    return Task.CompletedTask;
                var viewSize = _Driver.ViewportSize;
                activeTab.ScrollCursorInBounds(viewSize.X, viewSize.Y - _EditorView._TopMargin);
                _Driver.Invalidate();
                return Task.CompletedTask;
            }

            public Task OnMouseEventAsync(TerminalMouseEventType type, int x, int y)
            {
                if (type == TerminalMouseEventType.MouseDown)
                {
                    var activeTab = _EditorView._Editor.ActiveTab;
                    activeTab.SetCursorPositionFromHit(x, y);
                    _Driver.Invalidate();
                }
                return Task.CompletedTask;
            }

            public Task OnUserScrollAsync(int deltaX, int deltaY)
            {
                var activeTab = _EditorView._Editor.ActiveTab;
                var oldScrollX = activeTab.ScrollOffset.X;
                var oldScrollY = activeTab.ScrollOffset.Y;
                activeTab.Scroll(deltaX, deltaY);
                var viewSize = _Driver.ViewportSize;
                activeTab.ScrollTextInBounds(viewSize.X, viewSize.Y - _EditorView._TopMargin);
                if (oldScrollX != activeTab.ScrollOffset.X || oldScrollY != activeTab.ScrollOffset.Y)
                    _Driver.Invalidate();
                return Task.CompletedTask;
            }
        }
    }
}
