using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor
{
    public interface ITextEditorBuffer
    {
        public int Lines { get; }
        public int Length { get; }
        public int GetLineLength(int row);
        public string GetTextRange(int row, int col, int count);
        public void Insert(int row, int col, string text);
        public void InsertLineAfter(int row, int col);
        public void Delete(int row, int col, int count);
    }
}
