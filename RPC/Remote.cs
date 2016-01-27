using System;

namespace Zbus.RPC
{
    public class Remote : Attribute
    {
        private string id;

        public Remote(string id)
        {
            this.id = id;
        }

        public Remote()
        {
            this.id = null;
        }

        public string Id
        {
            get { return this.id; }
            set { this.id = value; }
        }
    }

}
