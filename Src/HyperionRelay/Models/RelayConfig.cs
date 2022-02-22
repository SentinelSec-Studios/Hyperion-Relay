using HyperionRelay.Enums;
using HyperionRelay.Infrastructure;

namespace HyperionRelay.Models
{
    public struct RelayConfig
    {
        // Do not change this value while the server is operating, unless you want hell to rain down!
        public ConnectionHandler? CurrentConnectionHandler { get; set; }
        public string? RelayIP { get; set; }
        public int RelayPort { get; set; }
        public LogLevel LogLevel { get; set; }
        public int MaxConnectionCount { get; set; }
    }
}
