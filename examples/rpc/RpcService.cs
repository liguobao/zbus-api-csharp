using System;  

using Zbus.Broker;
using Zbus.RPC;

namespace Zbus.Examples
{
    class User
    {
        public string name;
        public string addr;
    }
   
    class MyService
    {
        [Remote]
        public string getString(string msg)
        {
            return msg;
        }

        [Remote]
        public int plus(int a, int b)
        {
            return a + b;
        }

        [Remote]
        public User user(string name)
        { 
            User user = new User();
            user.name = name;
            user.addr = "深圳";
            return user;
        } 
    }
    
    class RpcServiceTest
    {
        public static void Main(string[] args)
        { 
            IBroker broker = new SingleBroker(); //using default configuration

            RpcProcessor processor = new RpcProcessor(new MyService());
            ServiceConfig config = new ServiceConfig(broker);
            config.Mq = "MyRpc"; //Service entry in zbus as a MQ
            config.MessageProcessor = processor;
            config.ConsumerCount = 32;
 
            Service service = new Service(config);
            service.Start();
            service.Stop();
            
            broker.Dispose(); 
            
            //Console.ReadKey();
        } 
    }
}
