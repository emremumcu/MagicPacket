using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicPacket
{
    [Serializable]
    public class PacketInfo
    {
        public string Target_MAC { get; set; }
        public string Target_IP { get; set; }
        public string SubnetMask { get; set; }
        public int Port { get; set; }
    }
}
