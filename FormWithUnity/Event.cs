using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormWithUnity
{
    public class Event: EventArgs
    {
        private string message;

        public Event(string message)
        {
            this.message = message;
        }
        
        // This is a straightforward implementation for 
        // declaring a public field
        public string Message
        {
            get
            {
                return message;
            }
        }//*/

    }

    
}// credit: https://www.codeproject.com/Articles/9355/Creating-advanced-C-custom-events
