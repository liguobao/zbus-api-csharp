
using Zbus.Mq;

namespace Zbus.RPC
{
    public class RpcConfig : MqConfig
    {
        public static readonly string DEFAULT_ENCODING = "UTF-8";
        public string module = "";
        public int timeout = 10000;
        public string encoding = DEFAULT_ENCODING;
    }
}
