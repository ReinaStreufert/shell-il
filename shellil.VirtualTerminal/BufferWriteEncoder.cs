using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class BufferWriteEncoder : IBufferWriteEncoder
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
            _BGInvalidated = false;
            _FGInvalidated = false;
            _PosInvalidated = false;
            _LastWrittenChar = c;
        }

        public void Encode(MemoryStream16 ms)
        {
            if (_BGInvalidated || _FGInvalidated || _PosInvalidated)
            {
                Write((char)0);
            }
            ms.Write((ushort)_CommandCount);
            var textSegment = _Text[0];
            var bgcSegment = _BackgroundColor[0];
            var fgcSegment = _ForegroundColor[0];
            var posSegment = _Position[0];
            for (int i = 0; i < _CommandCount; i++)
            {
                EnsureSegmentAlignment(_Position, ref posSegment, i);
                EnsureSegmentAlignment(_BackgroundColor, ref bgcSegment, i);
                EnsureSegmentAlignment(_ForegroundColor, ref fgcSegment, i);
                EnsureSegmentAlignment(_Text, ref textSegment, i);
                var offsetInPosSeg = i - posSegment.CommandIndexOffset;
                var offsetInBgcSeg = i - bgcSegment.CommandIndexOffset;
                var offsetInFgcSeg = i - fgcSegment.CommandIndexOffset;
                var offsetInTextSeg = i - textSegment.CommandIndexOffset;

                if (offsetInPosSeg == 0)
                    posSegment.WriteHead(ms);
                if (offsetInPosSeg >= posSegment.UnbrokenCount)
                {
                    var seekPosition = posSegment.ContiguousBreaking[offsetInPosSeg - posSegment.UnbrokenCount];
                    ms.Write(seekPosition.X);
                    ms.Write(seekPosition.Y);
                }
                if (offsetInBgcSeg == 0)
                    bgcSegment.WriteHead(ms);
                if (offsetInBgcSeg >= bgcSegment.UnbrokenCount)
                {
                    var uniqueBackgroundColor = bgcSegment.ContiguousBreaking[offsetInBgcSeg - bgcSegment.UnbrokenCount];
                    uniqueBackgroundColor.Encode(ms);
                }
                if (offsetInFgcSeg == 0)
                    fgcSegment.WriteHead(ms);
                if (offsetInFgcSeg >= fgcSegment.UnbrokenCount)
                {
                    var uniqueForegroundColor = fgcSegment.ContiguousBreaking[offsetInFgcSeg - fgcSegment.UnbrokenCount];
                    uniqueForegroundColor.Encode(ms);
                }
                if (offsetInTextSeg == 0)
                    textSegment.WriteHead(ms);
                if (offsetInTextSeg >= textSegment.UnbrokenCount)
                    ms.Write((ushort)textSegment.ContiguousBreaking[offsetInTextSeg - textSegment.UnbrokenCount]);
            }
        }

        private void EnsureSegmentAlignment<T>(RLEPartitioner<T> partitioner, ref RLESegment<T> currentSegment, int commandOffset) where T : notnull
        {
            var offsetInSegment = commandOffset - currentSegment.CommandIndexOffset;
            if (offsetInSegment >= currentSegment.UnbrokenCount + currentSegment.ContiguousBreaking.Count)
                currentSegment = partitioner[currentSegment.SegmentIndex + 1];
        }

        private class RLEPartitioner<T> where T : notnull
        {
            public int SegmentCount => _Segments.Count;
            public int CommandOffset
            {
                get {
                    if (_LastSegment == null)
                        return 0;
                    return _LastSegment.CommandIndexOffset + 
                        _LastSegment.UnbrokenCount +
                        _LastSegment.ContiguousBreaking.Count;
                }
            }
            public RLESegment<T> this[int index] => _Segments[index];

            private List<RLESegment<T>> _Segments = new List<RLESegment<T>>();
            private RLESegment<T>? _LastSegment => _Segments.Count > 0 ? _Segments[_Segments.Count - 1] : null;

            public void WriteRepitition()
            {
                if (_LastSegment != null && _LastSegment.ContiguousBreaking.Count == 0)
                    _LastSegment.UnbrokenCount++;
                else
                {
                    var newSegment = new RLESegment<T>(CommandOffset, SegmentCount);
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
                    var newSegment = new RLESegment<T>(CommandOffset, SegmentCount);
                    newSegment.ContiguousBreaking.Add(uniqueValue);
                    _Segments.Add(newSegment);
                }
            }
        }

        private class RLESegment<T> where T : notnull
        {
            public int SegmentIndex { get; }
            public int CommandIndexOffset { get; }
            public int UnbrokenCount { get; set; } = 0;
            public List<T> ContiguousBreaking { get; } = new List<T>();

            public RLESegment(int offset, int segmentIndex)
            {
                CommandIndexOffset = offset;
                SegmentIndex = segmentIndex;
            }

            public void WriteHead(MemoryStream16 ms)
            {
                ms.Write((ushort)UnbrokenCount);
                ms.Write((ushort)ContiguousBreaking.Count);
            }
        }
    }
}
