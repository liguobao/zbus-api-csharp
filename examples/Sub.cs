using System; 

using Zbus.Mq;
using Zbus.Net;
using Zbus.Broker;

namespace Zbus.Examples
{ 
    class SubTest
    {
        public static void Main(string[] args)
        {

            BrokerConfig config = new BrokerConfig();
            config.BrokerAddress = "127.0.0.1:15555";
            IBroker broker = new SingleBroker(config);

            Consumer c = new Consumer(broker, "MyPubSub", MqMode.PubSub);
            c.Topic = "sse";

            while (true)
            {
                Message msg = c.Recv(30000);
                if (msg == null) continue;

                System.Console.WriteLine(msg);
            }

            c.Dispose();
            broker.Dispose();
            Console.ReadKey();
        }
    }
}
