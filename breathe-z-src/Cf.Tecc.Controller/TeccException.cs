using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cf.Tecc.Controller
{
    [Serializable]
    public class TeccException : Exception
    {
        public TeccException(string message) : base(message)
        {
        }
    }
}
