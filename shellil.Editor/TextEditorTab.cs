using shellil.VirtualTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor
{
    public class TextEditorTab
    {
        public string? SourceFilePath => _SourceFilePath;
        public ITextEditorBuffer Buffer => _Buffer;
        public TerminalPosition ScrollOffset => _ScrollOffset;
        public TerminalPosition CursorPosition => _CursorPosition;

        public TextEditorTab()
        {

        }

        private string? _SourceFilePath;
        private ITextEditorBuffer _Buffer = new TextEditorBuffer();
        private TerminalPosition _ScrollOffset = new TerminalPosition(0, 0);
        private TerminalPosition _CursorPosition = new TerminalPosition(0, 0);
        private object _Sync = new object();

        public void Open(string sourcePath)
        {
            var buffer = new TextEditorBuffer(sourcePath);
            lock (_Sync)
            {
                _Buffer = buffer;
                _ScrollOffset = new TerminalPosition(0, 0);
                _CursorPosition = new TerminalPosition(0, 0);
                _SourceFilePath = sourcePath;
            }
        }

        public void Save(string dstPath)
        {
            var buffer = _Buffer;
            string text;
            lock (_Sync)
                text = buffer.GetTextRange(0, 0, buffer.Length);
            File.WriteAllText(dstPath, text);
            lock (_Sync)
            {
                if (buffer == _Buffer)
                    _SourceFilePath = dstPath;
            }
        }

        public void Scroll(int offsetX, int offsetY)
        {
            lock (_Sync)
            {
                _ScrollOffset.X = (ushort)(_ScrollOffset.X + offsetX);
                _ScrollOffset.Y = (ushort)(_ScrollOffset.Y + offsetY);
            }
        }

        public void ScrollTextInBounds(int width, int height)
        {
            lock (_Sync)
            {
                int maxLineLen = 0;
                for (int i = 0; i < _Buffer.Lines; i++)
                    maxLineLen = Math.Max(maxLineLen, _Buffer.GetLineLength(i));
                int maxScrollX = 0;
                if (maxLineLen > width)
                    maxScrollX = maxLineLen - width;
                int maxScrollY = 0;
                if (_Buffer.Lines > height)
                    maxScrollY = _Buffer.Lines - height;
                _ScrollOffset.X = (ushort)Math.Min(_ScrollOffset.X, maxScrollX);
                _ScrollOffset.Y = (ushort)Math.Min(_ScrollOffset.Y, maxScrollY);
            }
        }

        public void ScrollCursorInBounds(int width, int height)
        {
            lock (_Sync)
            {
                var scrollRight = _ScrollOffset.X + width - 1;
                var scrollBottom = _ScrollOffset.Y + height - 1;
                if (_CursorPosition.X > scrollRight)
                    _ScrollOffset.X = (ushort)(_CursorPosition.X - width + 1);
                else if (_CursorPosition.X < _ScrollOffset.X)
                    _CursorPosition.X = _ScrollOffset.X;
                if (_CursorPosition.Y > scrollBottom)
                    _ScrollOffset.Y = (ushort)(_CursorPosition.Y - height + 1);
            }
        }

        public int SeekHorizontal(int offset)
        {
            lock (_Sync)
            {
                var direction = offset > 0 ? 1 : -1;
                for (int i = 0; i < Math.Abs(offset); i++)
                {
                    var newX = _CursorPosition.X + direction;
                    if (newX > _Buffer.GetLineLength(_CursorPosition.Y))
                    {
                        if (_CursorPosition.Y + 1 >= _Buffer.Lines)
                            return i * direction;
                        _CursorPosition.Y++;
                        _CursorPosition.X = 0;
                    }
                    else if (newX < 0)
                    {
                        if (_CursorPosition.Y == 0)
                            return i * direction;
                        _CursorPosition.Y--;
                        _CursorPosition.X = (ushort)_Buffer.GetLineLength(_CursorPosition.Y);
                    }
                    else _CursorPosition.X = (ushort)newX;
                }
                return offset;
            }
        }

        public int SeekVertical(int offset)
        {
            lock (_Sync)
            {
                if (offset < 0)
                    offset = Math.Max(-_CursorPosition.Y, offset);
                else
                    offset = Math.Min(_Buffer.Lines - _CursorPosition.Y - 1, offset);
                _CursorPosition.Y = (ushort)(_CursorPosition.Y + offset);
                var lineLength = _Buffer.GetLineLength(_CursorPosition.Y);
                if (_CursorPosition.X > lineLength)
                    _CursorPosition.X = (ushort)lineLength;
                return offset;
            }
        }

        public void SetCursorPositionFromHit(int hitPointX, int hitPointY)
        {
            lock (_Sync)
            {
                var bufferX = _ScrollOffset.X + hitPointX;
                var bufferY = _ScrollOffset.Y + hitPointY;
                if (bufferY >= _Buffer.Lines)
                    bufferY = _Buffer.Lines - 1;
                var lineLength = _Buffer.GetLineLength(bufferY);
                if (bufferX > lineLength)
                    bufferX = lineLength;
                _CursorPosition.X = (ushort)bufferX;
                _CursorPosition.Y = (ushort)bufferY;
            }
        }

        public void Write(char c)
        {
            lock (_Sync)
                _Buffer.Insert(_CursorPosition.Y, _CursorPosition.X, c.ToString());
            SeekHorizontal(1);
        }

        public void WriteLineBreak()
        {
            lock (_Sync)
            {
                _Buffer.InsertLineAfter(_CursorPosition.Y, _CursorPosition.X);
                _CursorPosition.X = 0;
                _CursorPosition.Y++;
            }
        }

        public bool WriteBackspace()
        {
            lock (_Sync)
            {
                var newX = _CursorPosition.X - 1;
                var newY = _CursorPosition.Y;
                if (newX < 0)
                {
                    if (newY == 0)
                        return false;
                    newY--;
                    newX = _Buffer.GetLineLength(newY);
                }
                _Buffer.Delete(newY, newX, 1);
                _CursorPosition.X = (ushort)newX;
                _CursorPosition.Y = (ushort)newY;
                return true;
            }
        }
    }
}
