using HyperionRelay.Enums;
using HyperionRelay.Exceptions;
using System.Net;

namespace HyperionRelay.Models
{
    public class ClientSession
    {
        /// <summary>
        /// The ID assigned to the connected client.
        /// </summary>
        public Guid? ClientGuid { get; set; }

        /// <summary>
        /// The Internet Protocol (IP) associated with the connected client.
        /// </summary>
        public IPAddress? ClientEndpoint { get; set; }

        /// <summary>
        /// That state of the current session, this value is true by default unless a security violation is commited by the client making the value false.
        /// </summary>
        public AuthenticityState? SessionAuthenticityState { get; private set; }


        public ClientSession(Guid clientGuid)
        {
            ClientGuid = clientGuid;
            SessionAuthenticityState = AuthenticityState.AwaitingOnboarding;
        }

        /// <summary>
        /// Acts as a setter for safely and securely revoking client sessions when a security violation occurs. 
        /// </summary>
        public void RevokeSessionAuthenticity(string logMessage = "None Provided")
        {
            Console.WriteLine($"[Client Handler]: A security violation has been commited by client: {ClientGuid}!");
            Console.WriteLine($"[Client Handler]: Revoking session trust and disconnecting client!");
            Console.WriteLine($"[Client Handler]: Revoke Reason: {logMessage}");
            SessionAuthenticityState = AuthenticityState.Untrusted;
            throw new SecurityViolationException($"A security violation has been commited by client: {ClientGuid}!");
        }

        /// <summary>
        /// Setter for updating the value of authenticity the session in context.
        /// </summary>
        public void UpdateSessionAuthenticity(AuthenticityState authenticityState)
        {
            SessionAuthenticityState = authenticityState;
        }
    }
}
