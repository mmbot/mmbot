using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MMBot
{

    /// need to create an event using the emit method if it doesn't exist 
    /// each call to emit needs to invoke the emit event using a key string
    /// each call to On subscribes a passed delegate to an event handler
    /// this is a total mess

    public abstract class EventEmitter
    {
        private Dictionary<string, Delegate> eventTable = new Dictionary<string,Delegate>();

        public event EventHandler<EmitEventArgs> EmitEvent;

        public virtual void Emit(string key, OnData data)
        {
            EventHandler<EmitEventArgs> handler;
            if (!eventTable.ContainsKey(key))
                eventTable.Add(key, handler);

            if (null != (handler = (EventHandler<EmitEventArgs>)eventTable[key]))
            {
                handler(this);
            }
        }

        public static void SubscriberDelegate(OnData data)
        {
            //what
        }

        public virtual void On(string key, OnData data)
        {
            if (eventTable.ContainsKey(key))
            {
                eventTable[key] += new EventHandler(SubscriberDelegate(data));
            }
        }

        private virtual void HandleEmit(Delegate subscriber)
        {

        }
    }

    public class EventEmitItem
    {
        public event EventHandler EmitEvent;

        
    }

    //gonna need user object
    //data as array of objects? json?
    public class OnData
    {

    }

    public class EmitEventArgs : EventArgs
    {
        
    }

}
