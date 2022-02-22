using System;
using System.Runtime.Serialization;

namespace HyperionRelay.Exceptions
{
    [Serializable]
    public class GatewayDisconnectedException : Exception
    {
        // Serialize the exception to allow usage outside of app domain.
        public GatewayDisconnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        // Sets and allows changing of exception message from caller.
        public GatewayDisconnectedException(string message = "The connection to the gateway server has failed!") : base(message)
        {
        }
    }

    [Serializable]
    public class GatewayTimeOutException : Exception
    {

        public GatewayTimeOutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public GatewayTimeOutException(string message = "Connection to gateway server timed out whilst waiting for a message!") : base(message)
        {
        }
    }

    [Serializable]
    public class InsecureGatewayException : Exception
    {

        public InsecureGatewayException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InsecureGatewayException(string message = "A security violation has been thrown for the current session!") : base(message)
        {
        }
    }

    [Serializable]
    public class GatewayIdentificationFailed : Exception
    {

        public GatewayIdentificationFailed(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public GatewayIdentificationFailed(string message = "An issue occurred whilst attempting to identify the gateway session!") : base(message)
        {
        }
    }

    [Serializable]
    public class GatewayUnavaliableException : Exception
    {

        public GatewayUnavaliableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public GatewayUnavaliableException(string message = "An issue occurred whilst attempting to connect to the gateway!") : base(message)
        {
        }
    }
}
