using System;

namespace FormWithUnity
{
    /// <summary>
    /// The delegate procedure we are assigning to our object
    /// </summary>
    public class Event: EventArgs
    {
        public Event(string message)
        {
            this.Message = message;
        }

        // This is a straightforward implementation for 
        // declaring a public field
        public string Message { get; }

    }

    
}// credit: https://www.codeproject.com/Articles/9355/Creating-advanced-C-custom-events
