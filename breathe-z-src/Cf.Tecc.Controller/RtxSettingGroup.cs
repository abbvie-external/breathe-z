using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cf.Tecc.Controller
{
    [Serializable]
    public class RtxSettingGroup
    {
        public List<RtxSetting> Settings { get; set; }

        public RtxSettingGroup()
        {
            Settings = new List<RtxSetting>();
        }
    }
}
