using System;
using System.Runtime.Serialization;

namespace HyperionRelay.Exceptions
{
    [Serializable]
    public class SecurityViolationException : Exception
    {
        // Serialize the exception to allow usage outside of app domain.
        public SecurityViolationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        // Sets and allows changing of exception message from caller.
        public SecurityViolationException(string message = "The connected client has violated Gateway security protocol.") : base(message)
        {
        }
    }

}
