using HyperionRelay.Startup;
using HyperionRelay.Utilities;
using System.Text;

namespace HyperionRelay
{
    internal class HyperionRelay
    {
        public static string HyperionVersion = "0.1.0 (Developer Preview)";
        internal static ManualResetEvent? ServerShutdownCompleted { get; set; }
        internal static bool ShutdownRequested { get; set; }

        public static async Task Main()
        {
            Console.Title = "SentinelSec: Hyperion Relay  | Status: Initializing...";
            Console.CursorVisible = false;
            Console.WriteLine("Running pre-init validation.... ");
            await CheckForUpdatesAsync();
            await RelayStartup.InitRelayAsync();
        }

        internal static async Task CheckForUpdatesAsync()
        {
            HttpClient client = new();
            var beaconCheckResult = await client.GetAsync("https://raw.githubusercontent.com/SentinelSec-Development/TemporaryVersioningBeacon/main/HRVersion.md");
            var latestRelayVersion = await beaconCheckResult.Content.ReadAsStringAsync();
            latestRelayVersion = latestRelayVersion.Replace("\n", string.Empty);
            if (latestRelayVersion != HyperionVersion)
            {
                Console.WriteLine($"Found a new Hyperion Relay version: {latestRelayVersion}, Current: {HyperionVersion}");
                Console.WriteLine("There is a new version of HyperionRelay!");
                Console.WriteLine("Please update your relay now to receive the latest features, fixes, and security patches.");
                Console.WriteLine();
                Console.WriteLine("Automated features is in the works to allow for automated provisioning & updating in the future.");
                Console.ReadLine();
                Environment.Exit(1);
            }
        }
    }

}
