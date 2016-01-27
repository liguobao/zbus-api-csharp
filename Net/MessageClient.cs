using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;


using Zbus.Kit.Log;

namespace Zbus.Net
{
    public class MessageClient
    {
        private static readonly ILogger log = LoggerFactory.GetLogger(typeof(MessageClient));

        private TcpClient client;
        private string host = "127.0.0.1";
        private int port = 15555;
        private bool autoReconnect = true;
        private int reconnectInterval = 3000;

        private Stream stream; 
        private IoBuffer readBuf = new IoBuffer();

        /// <summary>
        /// 当前匹配的msgid
        /// </summary>
        private string msgidMatch = null;

        /// <summary>
        /// 消息发送结果记录
        /// </summary>
        private IDictionary<string, Message> resultTable = new Dictionary<string, Message>();

        private DateTime timeCreated = DateTime.Now;
        public DateTime TimeCreated
        {
            get { return timeCreated; }
            set { timeCreated = value; }
        }

        /// <summary>
        /// 初始化连接地址
        /// </summary>
        /// <param name="address"></param>
        public MessageClient(string address)
        {
            string[] blocks = address.Split(':');
            this.host = blocks[0];
            this.port = int.Parse(blocks[1]);
        }

        /// <summary>
        /// 是否可连接
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            if (client == null) return false;
            return client.Connected;
        }

        /// <summary>
        /// 创建TCP连接
        /// </summary>
        public void ConnectIfNeeded()
        {
            if (this.client != null) return;
            while (true) {
                try
                {
                    this.client = new TcpClient(this.host, this.port);
                    break;
                }
                catch (SocketException se)
                {
                    log.Error(se.Message, se);
                    if (this.autoReconnect)
                    {
                        log.DebugFormat("Failed to connecting {0}:{1}, try again in 3s", this.host, this.port);
                        Thread.Sleep(this.reconnectInterval);
                        continue;
                    }
                    else
                    {
                        throw se;
                    }  
                }
            }
            //log.DebugFormat("Connected to {0}:{1}", this.host, this.port);
            this.stream = this.client.GetStream();
        }

        /// <summary>
        /// 重置连接
        /// </summary>
        public void Reconnect()
        {
            if (this.client != null)
            {
                this.Close();
                this.client = null;
            }
            while (this.client == null)
            {

                try
                {
                    log.DebugFormat("Trying reconnect to ({0}:{1})", this.host, this.port);
                    ConnectIfNeeded();
                    log.DebugFormat("Connected to ({0}:{1})", this.host, this.port);
                }
                catch (SocketException se)
                {
                    this.client = null;
                    log.Error(se.Message, se);
                    if (this.autoReconnect)
                    {
                        Thread.Sleep(this.reconnectInterval);
                        continue;
                    }
                    else
                    {
                        throw se;
                    }
                }
            }

        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            if (this.client != null)
            {
                this.stream.Close();
                this.client.Close();
            }
        }

        /// <summary>
        /// 标记当前消息ID
        /// </summary>
        /// <param name="msg"></param>
        private void MarkMessage(Message msg)
        {
            if (msg.Id == null)
            {
                msg.Id = System.Guid.NewGuid().ToString();
            }
            this.msgidMatch = msg.Id;
        }

        public void Send(Message msg, int timeout)
        {
            this.ConnectIfNeeded();
            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Send: {0}", msg);
            }
            this.MarkMessage(msg);

            IoBuffer buf = new IoBuffer();
            msg.Encode(buf);
            this.stream.WriteTimeout = timeout;
            this.stream.Write(buf.Data, 0, buf.Position);
        }

        public Message Recv(int timeout)
        {
            this.ConnectIfNeeded(); 
            if (this.msgidMatch != null && this.resultTable.ContainsKey(this.msgidMatch))
            {
                Message msg = this.resultTable[this.msgidMatch];
                this.resultTable.Remove(this.msgidMatch);
                return msg;
            }
            this.client.ReceiveTimeout = timeout;

            while (true)
            {

                byte[] buf = new byte[4096];
                int n = this.stream.Read(buf, 0, buf.Length); 
                this.readBuf.Put(buf, 0, n);

                IoBuffer tempBuf = this.readBuf.Duplicate();
                tempBuf.Flip(); //to read mode
                Message msg = Message.Decode(tempBuf);
                if (msg == null)
                {
                    continue;
                }

                this.readBuf.Move(tempBuf.Position);

                if (this.msgidMatch != null && !this.msgidMatch.Equals(msg.Id))
                {
                    this.resultTable[this.msgidMatch] = msg;
                    continue;
                }

                if (log.IsDebugEnabled)
                {
                    log.DebugFormat("Recv: {0}", msg);
                }
                return msg;
            } 
        }

        public Message Invoke(Message msg, int timeout)
        {
            this.Send(msg, timeout);
            //绑定事件，东西回来的时候触发事件
            return this.Recv(timeout);
        } 
      
    }
}
