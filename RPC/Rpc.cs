using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 
using Zbus.Kit.Json;
using Zbus.Mq;
using Zbus.Net;
using Zbus.Broker;

namespace Zbus.RPC
{
    public class Rpc : MqAdmin
    {
        private String module = "";
        private String encoding = RpcConfig.DEFAULT_ENCODING;
        private int timeout = 10000;  

        public Rpc(IBroker broker, String mq, params MqMode[] modes)
            :base(broker, mq, modes)
        {
        }

        public Rpc(RpcConfig config)
            :base(config)
        {
            this.module = config.module;
            this.encoding = config.encoding;
            this.timeout = config.timeout;
        }

        private Message Invoke(Message msg, int timeout)
        {
            msg.Cmd = Proto.Produce;
            msg.Mq = this.mq;
            msg.Ack = false;

            return this.broker.InvokeSync(msg, timeout);
        }

        public object Invoke(string method, params object[] args)
        {
            IDictionary<string, object> req = new Dictionary<string, object>();
            req["module"] = this.module;
            req["method"] = method;
            req["params"] = args;
            req["encoding"] = this.encoding;

            Message msgReq = new Message();
            string json = JSON.Instance.ToJSON(req);
            msgReq.SetJsonBody(json);

            Message msgRes = this.Invoke(msgReq, this.timeout);
            string encodingName = msgRes.Encoding;
            Encoding encoding = Encoding.Default;
            if(encodingName != null){
                encoding = Encoding.GetEncoding(encodingName);
            }
            string jsonString = msgRes.GetBody(encoding);
            Dictionary<string, object> jsonRes = (Dictionary<string, object>)JSON.Instance.Parse(jsonString);
            if (jsonRes.Keys.Contains("result"))
            {
                return jsonRes["result"];
            }

            if (jsonRes.Keys.Contains("error"))
            {
                throw new RpcException((string)jsonRes["error"]);
            }
            throw new RpcException("return format error");

        }
    } 

}
