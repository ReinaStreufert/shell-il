using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor
{
    public interface IUserPrompt
    {
        public string Caption { get; }
        public void Fulfill(string inputText);
    }
}
