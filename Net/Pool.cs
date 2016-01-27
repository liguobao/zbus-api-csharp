using System;
using System.Collections.Generic;

namespace Zbus.Net
{
    public class MessageClientPoolConfig
    {
        private string brokerAddress = "127.0.0.1:15555"; 
        private int maxSize = 20;
        private int clientLifeTimeInMinutes = 10;

        public string BrokerAddress
        {
            get { return brokerAddress; }
            set { brokerAddress = value; }
        }

        public int MaxSize
        {
            get { return maxSize; }
            set { maxSize = value; }
        }

        public int ClientLifeTimeInMinutes
        {
            get { return clientLifeTimeInMinutes; }
            set { clientLifeTimeInMinutes = value; }
        }
    }

    public class MessageClientPool : IDisposable
    {
        private string brokerAddress; 
        private int maxSize = 20;
        private int clientLifeTimeInMinutes = 10;

        private Queue<MessageClient> clients = new Queue<MessageClient>();

        /// <summary>
        /// 初始化消息连接池
        /// </summary>
        /// <param name="config"></param>
        public MessageClientPool(MessageClientPoolConfig config)
        {
            brokerAddress = config.BrokerAddress; 
            maxSize = config.MaxSize;
            clientLifeTimeInMinutes = config.ClientLifeTimeInMinutes;
        }

        /// <summary>
        /// 取出一个MessageClient
        /// </summary>
        /// <returns></returns>
        public MessageClient BorrowClient()
        {
            if (clients.Count > 0)
            {
                lock (clients)
                {
                    MessageClient client = null;
                    while (clients.Count > 0)
                    {
                        client = clients.Dequeue();
                        if (client.IsConnected())
                        {
                            return client;
                        }
                        client.Close();
                    }
                }
            }
            return OpenClient();
        }

        /// <summary>
        /// 添加一个MessageClient到队列中
        /// </summary>
        /// <param name="client"></param>
        public void ReturnClient(MessageClient client)
        {
            if(client == null) return;
            lock (clients)
            {
                TimeSpan lifeTime = DateTime.Now.Subtract(client.TimeCreated);
                if (clients.Count < maxSize && lifeTime.Minutes < clientLifeTimeInMinutes)
                {
                    if (client.IsConnected())
                    {
                        clients.Enqueue(client);
                    }
                    else
                    {
                        client.Close();
                    }
                }
                else
                {
                    client.Close();
                }
            }

        }

        private MessageClient OpenClient()
        {
            if (clients.Count > maxSize)
            {
                throw new Exception("RemotingClientPool reached its limit");
            }
            MessageClient client = new MessageClient(brokerAddress);
            return client;
        }

        /// <summary>
        /// 销毁所有的MessageClient
        /// </summary>
        public void Dispose()
        {
            while (clients.Count > 0)
            {
                MessageClient client = clients.Dequeue();
                client.Close();
            }

        }
    }


}
