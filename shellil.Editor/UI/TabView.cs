using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor.UI
{
    public class TabView : ITerminalUIComponent
    {
        public TabView(TextEditor editor)
        {
            _Editor = editor;
        }
        
        private TextEditor _Editor;
        private int _LastCursorX = -1;

        public void Render(IFrameRenderContext context)
        {
            var activeTab = _Editor.ActiveTab;
            var tabCount = _Editor.TabCount;
            var tabWidthD = context.FrameWidth / (double)tabCount;
            var frameBuffer = context.FrameBuffer;
            var appearance = _Editor.Appearance;
            for (int i = 0; i < tabCount; i++)
            {
                var tabLeft = (int)Math.Floor(i * tabWidthD);
                var tabRight = (int)Math.Floor((i + 1) * tabWidthD) - 1;
                var tab = _Editor.GetTab(i);
                if (tab == activeTab)
                {
                    frameBuffer.SetBackgroundColor(appearance.SelectedTabBackground);
                    frameBuffer.SetForegroundColor(appearance.SelectedTabForeground);
                } else
                {
                    frameBuffer.SetBackgroundColor(appearance.TabBackground);
                    frameBuffer.SetForegroundColor(appearance.TabForeground);
                }
                var textAreaWidth = tabRight - tabLeft;
                frameBuffer.SetCursorPosition(new TerminalPosition(tabLeft, 0));
                frameBuffer.Write(GetTabTitle(tab, textAreaWidth));
                if (_LastCursorX == tabRight)
                    frameBuffer.SetForegroundColor(appearance.TabCloseHighlight);
                frameBuffer.Write("X");
                var tabInputHandler = new TabInputHandler(this, tab, context.Driver, tabLeft, tabRight);
                context.ClaimInputRegion(new TerminalPosition(tabLeft, 0), new TerminalPosition(tabRight - tabLeft + 1, 1), tabInputHandler);
            }
        }

        private string GetTabTitle(TextEditorTab tab, int length)
        {
            string text;
            if (tab.SourceFilePath == null)
                text = "Untitled";
            else
                text = Path.GetFileName(tab.SourceFilePath);
            if (text.Length > length)
                return text.Substring(0, length - 3) + "...";
            else if (text.Length < length)
                return text + new string(' ', length - text.Length);
            else
                return text;
        }

        private class TabInputHandler : IFocusable
        {
            public bool CanFocus => false;
            public TerminalPosition ShownCursorPosition => throw new NotImplementedException();
            public TerminalCursorState ShownCursorState => throw new NotImplementedException();

            public TabInputHandler(TabView tabView, TextEditorTab tab, IFrameDriver driver, int tabLeft, int tabRight)
            {
                _TabView = tabView;
                _Tab = tab;
                _Driver = driver;
                _TabLeft = tabLeft;
                _TabRight = tabRight;
            }

            private TabView _TabView;
            private TextEditorTab _Tab;
            private IFrameDriver _Driver;
            private int _TabLeft;
            private int _TabRight;

            public void OnFocused()
            {
                throw new NotImplementedException();
            }

            public void OnLostFocus()
            {
                throw new NotImplementedException();
            }

            public Task OnInputCharAsync(char inputChar, TerminalModifierKeys modifiers)
            {
                throw new NotImplementedException();
            }

            public Task OnMouseEventAsync(TerminalMouseEventType type, int x, int y)
            {
                var viewX = _TabLeft + x;
                if (type == TerminalMouseEventType.MouseDown)
                {
                    _TabView._LastCursorX = viewX;
                    if (viewX == _TabRight)
                        _TabView._Editor.CloseTab(_Tab);
                    else
                        _TabView._Editor.ActiveTab = _Tab;
                    _Driver.Invalidate();
                } else if (type == TerminalMouseEventType.MouseMove)
                {
                    var oldCloseHover = _TabView._LastCursorX == _TabRight;
                    var closeHover = viewX == _TabRight;
                    _TabView._LastCursorX = viewX;
                    if (oldCloseHover != closeHover)
                        _Driver.Invalidate();
                }
                return Task.CompletedTask;
            }

            public Task OnSpecialKeyAsync(TerminalSpecialKey key, TerminalModifierKeys modifiers)
            {
                throw new NotImplementedException();
            }

            public Task OnUserScrollAsync(int deltaX, int deltaY)
            {
                throw new NotImplementedException();
            }
        }
    }
}
