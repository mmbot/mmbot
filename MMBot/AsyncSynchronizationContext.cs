using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MMBot
{
    public class AsyncSynchronizationContext : SynchronizationContext
    {
        public override void Send(SendOrPostCallback d, object state)
        {
            try
            {
                d(state);
            } catch (Exception ex)
            {
                // Put your exception handling logic here.

                Console.WriteLine(ex.Message);
            }
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            try
            {
                d(state);
            } catch (Exception ex)
            {
                // Put your exception handling logic here.

                Console.WriteLine(ex.Message);
            }
        }
    }
}
