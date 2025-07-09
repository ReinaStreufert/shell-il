using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.VirtualTerminal
{
    public class MemoryStream16
    {
        const int _InitialLength = 16;

        private ushort[] _Buffer = new ushort[_InitialLength];
        private int _Position;

        public void Write(ushort value)
        {
            if (_Position >= _Buffer.Length)
                Array.Resize(ref _Buffer, _Buffer.Length * 2);
            _Buffer[_Position++] = value;
        }

        public ushort[] ToArray()
        {
            if (_Buffer.Length > _Position)
                Array.Resize(ref _Buffer, _Position);
            return _Buffer;
        }
    }
}
