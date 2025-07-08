using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class BufferWriteEncoder
    {
        private RLEPartitioner<char> _Text = new RLEPartitioner<char>();
        private RLEPartitioner<TerminalColor> _BackgroundColor = new RLEPartitioner<TerminalColor>();
        private RLEPartitioner<TerminalColor> _ForegroundColor = new RLEPartitioner<TerminalColor>();
        private RLEPartitioner<TerminalPosition> _Position = new RLEPartitioner<TerminalPosition>();
        private TerminalColor? _LastBGColor;
        private TerminalColor? _LastFGColor;
        private TerminalPosition? _LastSeekPosition;
        private bool _BGInvalidated = false;
        private bool _FGInvalidated = false;
        private bool _PosInvalidated = false;
        private char _LastWrittenChar = ' ';
        private int _CommandCount;

        public void SetCursorPosition(TerminalPosition position)
        {
            _LastSeekPosition = position;
            _PosInvalidated = true;
        }

        public void SetBackgroundColor(TerminalColor color)
        {
            if (_LastBGColor == color)
                return;
            _LastBGColor = color;
            _BGInvalidated = true;
        }

        public void SetForegroundColor(TerminalColor color)
        {
            if ( _LastFGColor == color) 
                return;
            _LastFGColor = color;
            _FGInvalidated = true;
        }

        public void Write(string s)
        {
            foreach (var c in s)
                Write(c);
        }

        public void Write(char c)
        {
            var charInvalidated = _LastWrittenChar != c;
            if (_BGInvalidated)
                _BackgroundColor.WriteUnique(_LastBGColor!);
            else
                _BackgroundColor.WriteRepitition();
            if (_FGInvalidated)
                _ForegroundColor.WriteUnique(_LastFGColor!);
            else
                _ForegroundColor.WriteRepitition();
            if (_PosInvalidated)
                _Position.WriteUnique(_LastSeekPosition!);
            else _Position.WriteRepitition();
            if (charInvalidated)
                _Text.WriteUnique(c);
            else
                _Text.WriteRepitition();
            _CommandCount++;
        }

        public void Encode(Stream outputStream)
        {
            using (BinaryWriter bw = new BinaryWriter(outputStream))
            {
                bw.Write((ushort)_CommandCount);
            }
        }

        private class RLEPartitioner<T> where T : notnull
        {
            private List<RLESegment<T>> _Segments = new List<RLESegment<T>>();
            private RLESegment<T>? _LastSegment => _Segments.Count > 0 ? _Segments[_Segments.Count - 1] : null;

            public void WriteRepitition()
            {
                if (_LastSegment != null && _LastSegment.ContiguousBreaking.Count == 0)
                    _LastSegment.UnbrokenCount++;
                else
                {
                    var newSegment = new RLESegment<T>();
                    newSegment.UnbrokenCount++;
                    _Segments.Add(newSegment);
                }
                
            }

            public void WriteUnique(T uniqueValue)
            {
                if (_LastSegment != null)
                    _LastSegment.ContiguousBreaking.Add(uniqueValue);
                else
                {
                    var newSegment = new RLESegment<T>();
                    newSegment.ContiguousBreaking.Add(uniqueValue);
                }
            }
        }

        private class RLESegment<T> where T : notnull
        {
            public int UnbrokenCount { get; set; } = 0;
            public List<T> ContiguousBreaking { get; } = new List<T>();
        }
    }
}
