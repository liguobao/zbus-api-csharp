﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
 
using Zbus.Kit.Log;
using Zbus.Broker;
using Zbus.Net;

namespace Zbus.Mq
{
    public interface IMessageHandler
    {
        void Handle(Message msg, Consumer consumer);
    }
    public class Consumer : MqAdmin, IDisposable
    {
        private static readonly ILogger log = LoggerFactory.GetLogger(typeof(Consumer));
        private MessageClient client = null;
        private string topic = null;
        private int consumeTimeout = 30000; //5 minutes

        public Consumer(IBroker broker, String mq, params MqMode[] modes)
            :base(broker, mq, modes)
        {
        }

        public Consumer(MqConfig config)
            :base(config)
        {
            this.topic = config.Topic;
        }

        public string Topic
        {
            set { topic = value;}
        }

        public int ConsumeTimeout
        {
            get { return consumeTimeout; }
            set { consumeTimeout = value; }
        }

        public Message Take()
        {
            Message msg = null;
            while (msg == null)
            {
                msg = Recv(this.consumeTimeout);
            }
            return msg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Message Recv(int timeout)
        {
            if (this.client == null)
            {
                this.client = broker.GetClient(GetClientHint());
            }
            Message req = new Message();
            req.Cmd = Proto.Consume;
            req.Mq = this.mq; 
            if ((this.mode & (int)MqMode.PubSub) != 0)
            {
                if (this.topic != null)
                {
                    req.Topic = this.topic;
                }
            }
            try
            { 
                Message res = this.client.Invoke(req, timeout);
                if (res != null && res.IsStatus404())
                {
                    if (!this.CreateMQ())
                    {
                        throw new MqException("register error");
                    }
                    return Recv(timeout);
                }
                if (res != null)
                {
                    res.Id = res.RawId;
                    res.RemoveHead(Message.RAWID);
                }
                return res;
            }
            catch (IOException ex)
            {
                Exception cause = ex.InnerException;
                if (cause is SocketException)
                {
                    SocketException se = (SocketException)cause;
                    if (se.SocketErrorCode == SocketError.TimedOut)
                    {
                        if (Environment.Version.Major < 4) //.net 3.5 socket sucks!!!!
                        {
                            this.HandleFailover();
                        }
                        return null; 
                    }
                    
                    if (se.SocketErrorCode == SocketError.Interrupted)
                    {
                        throw se;
                    }

                    // all other socket error reconnect by default
                    this.HandleFailover();
                    return null;
                }

                throw ex; 
            }   
        }

        public void Route(Message msg, int timeout)
        {
            msg.Cmd = Proto.Route;
            msg.Ack = false;
            this.client.Send(msg, timeout);
        }


        private void HandleFailover()
        {
            try
            {
                broker.CloseClient(this.client);
                this.client = broker.GetClient(GetClientHint());
            }
            catch (IOException ex)
            {
                log.Error(ex.Message, ex);
            }
            
        }

 

        private volatile Thread consumerThread = null;
        private volatile IMessageHandler consumerHandler = null;

        public void OnMessage(IMessageHandler handler)
        {
            this.consumerHandler = handler;
        }


        private void Run()
        {
            while (true)
            {
                try
                {
                    Message req = this.Recv(consumeTimeout);
                    if (req == null) continue;

                    if (consumerHandler == null)
                    {
                        log.Warn("Missing consumer MessageHandler, call OnMessage first");
                        continue;
                    }
                    consumerHandler.Handle(req, this);
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.Interrupted)
                    {
                        break;
                    }
                    log.Error(se.Message, se);
                }
                catch (ThreadInterruptedException e)
                {
                    break;
                }
                catch (System.Exception e)
                {
                    log.Error(e.Message, e);
                }
            }

        }

        public void Start()
        {
            if (this.consumerThread == null)
            {
                this.consumerThread = new Thread(this.Run);
            }
            if (this.consumerThread.IsAlive) return;
            this.consumerThread.Start();
        }

        
        public void Stop()
        {
            if (this.consumerThread != null)
            {
                this.consumerThread.Interrupt();
                this.consumerThread = null;
            }
            if (this.client != null)
            { 
                this.client.Close(); //destroy it, not return to pool
                //broker.CloseClient(this.client); 
            }
        }

        public void Dispose()
        {
            Stop(); 
        }
    }

}
