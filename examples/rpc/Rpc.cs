using Zbus.Broker;
using Zbus.RPC;
using System;
namespace Zbus.Examples
{ 
    class RpcClientTest
    { 
        public static void Main(string[] args)
        { 
            //1) create broker
            IBroker broker = new SingleBroker();

            RpcConfig config = new RpcConfig();
            config.Mq = "MyRpc"; //MQ entry in zbus serving for the RPC
            config.Broker = broker;
            //2) create Rpc object, 
            Rpc rpc = new Rpc(config);
            //3) invoke a method with params
            object res = rpc.Invoke("getString", "test");  
            
            broker.Dispose(); 
            Console.ReadKey();
        }
    }
}
