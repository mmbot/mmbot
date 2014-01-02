using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot
{
    public abstract class EventEmitter
    {
        protected Dictionary<string, EventEmitItem> EmitTable = new Dictionary<string, EventEmitItem>();

        public virtual void Emit(string key, object data)
        {
            if (EmitTable.ContainsKey(key))
                EmitTable[key].Raise(data);
        }

        public virtual void On(string key, Action<object> action)
        {
            if (!EmitTable.ContainsKey(key))
            {
                EmitTable.Add(key, new EventEmitItem());
            }

            EmitTable[key].Emitted += delegate(object o, EventArgs e) { action(o); };
        }
    }

    public class EventEmitItem
    {
        public event EventHandler Emitted;

        public void Raise(object data)
        {
            Emitted.Raise(data, null);
        }
    }
}
