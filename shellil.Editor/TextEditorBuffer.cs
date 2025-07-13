using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor
{
    public class TextEditorBuffer : ITextEditorBuffer
    {
        public int Lines => _Lines.Count;
        public int Length => _Lines.Select(sb => sb.Length).Sum() + Environment.NewLine.Length * _Lines.Count;

        public TextEditorBuffer(IEnumerable<string> lines)
        {
            _Lines.Clear();
            _Lines.AddRange(lines.Select(l => new StringBuilder(l)));
        }

        public TextEditorBuffer(string filePath) : this(File.ReadAllLines(filePath)) { }

        public TextEditorBuffer()
        {
            _Lines.Add(new StringBuilder());
        }

        private List<StringBuilder> _Lines = new List<StringBuilder>();

        public void Insert(int row, int col, string text)
        {
            var line = _Lines[row];
            line.Insert(col, text);
        }

        public void InsertLineAfter(int row, int col)
        {
            var line = _Lines[row];
            StringBuilder newLine = new StringBuilder();
            if (col < line.Length)
            {
                for (int i = col; i < line.Length; i++)
                    newLine.Append(line[i]);
                line.Remove(col, line.Length - col);
            }
            _Lines.Insert(row + 1, newLine);
        }

        public void Delete(int row, int col, int count)
        {
            for (; ; )
            {
                var line = _Lines[row];
                if (col + count <= line.Length)
                {
                    line.Remove(col, count);
                    break;
                }
                if (row + 1 < _Lines.Count)
                {
                    JoinLine(row + 1);
                    count--;
                }
                else throw new IndexOutOfRangeException(nameof(count));
            }
        }

        public int GetLineLength(int row)
        {
            return _Lines[row].Length;
        }

        public string GetTextRange(int row, int col, int count)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var line = _Lines[row];
                if (col < line.Length)
                {
                    sb.Append(line[col]);
                    col++;
                }
                else
                {
                    sb.AppendLine();
                    row++;
                    col = 0;
                }
            }
            return sb.ToString();
        }

        private void JoinLine(int line)
        {
            if (line < 1)
                throw new IndexOutOfRangeException(nameof(line));
            _Lines[line - 1].Append(_Lines[line].ToString());
            _Lines.RemoveAt(line);
        }
    }
}
