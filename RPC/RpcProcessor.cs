using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

using Zbus.Net;
using Zbus.Kit.Json;


namespace Zbus.RPC
{
    public class RpcProcessor : IMessageProcessor
    {

        private Encoding encoding;
        private Dictionary<string, MethodInstance> methods = new Dictionary<string, MethodInstance>();

        public RpcProcessor(params object[] services)
        {
            this.Init(Encoding.UTF8, services);
        }

        public RpcProcessor(Encoding encoding, params object[] services)
        {
            this.Init(encoding, services);
        }

        private void Init(Encoding encoding, params object[] services)
        {
            this.encoding = encoding;
            foreach (object service in services)
            {
                this.InitCommandTable(service);
            }
        }

        private void InitCommandTable(object service)
        {
            List<Type> types = new List<Type>();
            types.Add(service.GetType());
            foreach (Type type in service.GetType().GetInterfaces())
            {
                types.Add(type);
            }
            foreach (Type type in types)
            {
                foreach (MethodInfo info in type.GetMethods())
                {
                    foreach (Attribute attr in Attribute.GetCustomAttributes(info))
                    {
                        if (attr.GetType() == typeof(Remote))
                        {
                            Remote r = (Remote)attr;
                            string id = r.Id;
                            if (id == null)
                            {
                                id = info.Name;
                            }
                            if (this.methods.ContainsKey(id))
                            {
                                Console.WriteLine("{0} overridden", id);
                                break;
                            }

                            MethodInstance instance = new MethodInstance(info, service);
                            this.methods[id] = instance;
                            break;
                        }
                    }
                }
            }
        }

        public Message Process(Message request)
        {
            string json = request.GetBody();

            System.Exception error = null;
            object result = null;

            string method = null;
            ArrayList args = null;

            MethodInstance target = null;

            Dictionary<string, object> parsed = null;
            try
            {
                parsed = (Dictionary<string, object>)JSON.Instance.Parse(json);
            }
            catch (System.Exception ex)
            {
                error = ex;
            }
            if (error == null)
            {
                try
                {
                    method = (string)parsed["method"];
                    args = (ArrayList)parsed["params"];
                }
                catch (System.Exception ex)
                {
                    error = ex;
                }
                if (method == null)
                {
                    error = new RpcException("missing method name");
                }
            }

            if (error == null)
            {
                if (this.methods.ContainsKey(method))
                {
                    target = this.methods[method];
                }
                else
                {
                    error = new RpcException(method + " not found");
                }
            }

            if (error == null)
            {
                try
                {
                    ParameterInfo[] pinfo = target.Method.GetParameters();
                    if (pinfo.Length == args.Count)
                    {
                        object[] paras = new object[args.Count];
                        for (int i = 0; i < pinfo.Length; i++)
                        {
                            paras[i] = System.Convert.ChangeType(args[i], pinfo[i].ParameterType);
                        }
                        result = target.Method.Invoke(target.Instance, paras);
                    }
                    else
                    {
                        error = new RpcException("number of argument not match");
                    }
                }
                catch (System.Exception ex)
                {
                    error = ex;
                }
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            if (error == null)
            {
                data["error"] = null;
                data["result"] = result;
            }
            else
            {
                data["error"] = error.Message;
                data["result"] = null;
            }

            string resJson = JSON.Instance.ToJSON(data);
            Message res = new Message();
            res.SetBody(resJson);

            return res;
        }
    }


}
