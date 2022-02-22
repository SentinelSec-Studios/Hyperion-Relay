using HyperionRelay.Enums;
using HyperionRelay.Models;
using HyperionRelay.Utilities;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace HyperionRelay.Startup
{
    class RelayStartup
    {
        public static CancellationTokenSource? CancellationTokenSource { get; private set; }
        public static RelayConfig CurrentConfig;
        private static CancellationToken CancelToken { get; set; }

        /// <summary>
        /// Initializes Relay server sockets and command handler. 
        /// Blocks the current thread whilst the server is running unless cancellation is requested.
        /// </summary>
        /// <param name="cliArgs"></param>
        public static async Task InitRelayAsync()
        {
            Console.Clear();
            // Set up tokens for redundant restart.
            CancellationTokenSource = new CancellationTokenSource();
            CancelToken = CancellationTokenSource.Token;
            Console.CancelKeyPress += Console_CancelKeyPress;
            Console.Title = $"SentinelSec: Hyperion Relay | Build {HyperionRelay.HyperionVersion} | Initializing Relay...";
            DisplayBanner();

            Console.Write("Obtaining relay configuration...");
            ConsoleUtility.StartProgressSpinner();
            // Load or create the RelayConfig.json and validate/apply neccesary pre-execution settings
            CurrentConfig = new();
            CurrentConfig = await LoadRelayConfig();
            ValidateConfigurationAsync(CurrentConfig);
            ConsoleUtility.StopProgressSpinner();

            Console.WriteLine("Initializing server, please wait... ");
            // Spawn server in separate thread to avoid blocking main threads.
            CurrentConfig.CurrentConnectionHandler = new();
            if (IPAddress.TryParse(CurrentConfig.RelayIP!, out var ipAddress))
            {
                await Task.Run(async void () => await CurrentConfig.CurrentConnectionHandler.StartServer(new IPEndPoint(ipAddress, CurrentConfig.RelayPort)), CancelToken);
            }
            else
            {
                Console.WriteLine("Invalid server IP supplied!");
                Environment.Exit(3);
            }
            Console.WriteLine($"Hyperion-Relay v.{HyperionRelay.HyperionVersion} started and listening via endpoint: {CurrentConfig.RelayIP}:{CurrentConfig.RelayPort}.");
            // Clean up...
            CancelToken.WaitHandle.WaitOne();
            CancellationTokenSource.Dispose();
        }

        private static async Task<RelayConfig> LoadRelayConfig()
        {
            if (!File.Exists("RelayConfig.json"))
            {
                using var cfgFile = File.Create("RelayConfig.json");
                var options = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize(new RelayConfig() {RelayIP = "127.0.0.1", RelayPort = 9696, LogLevel = LogLevel.Nothing, MaxConnectionCount = 25}, options);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
                await cfgFile.WriteAsync(jsonBytes);
            }

            using FileStream stream = File.OpenRead("RelayConfig.json");
            return await JsonSerializer.DeserializeAsync<RelayConfig>(stream);
        }


        public static void ValidateConfigurationAsync(RelayConfig relayConfiguration)
        {
            PropertyInfo[] info = relayConfiguration.GetType().GetProperties();
            for (int i = 0; i < info.Length; i++)
            {
                switch (info[i].Name)
                {
                    case nameof(RelayConfig.CurrentConnectionHandler):
                        continue;

                    case nameof(RelayConfig.RelayIP):
                        var iPStr = Convert.ToString(info[i].GetValue(relayConfiguration));
                        if (string.IsNullOrEmpty(iPStr))
                        {
                            relayConfiguration.RelayIP = "127.0.0.1";
                        }
                        continue;

                    case nameof(RelayConfig.RelayPort):
                        var iPortStr = Convert.ToString(info[i].GetValue(relayConfiguration));
                        if (int.TryParse(iPortStr, out int port))// Port was not a valid integer.
                        {
                            if (port < 65535 && port > 0)
                                continue;
                        }// Port was out of range of possible ports or invalid.
                        relayConfiguration.RelayPort = 9696;
                        continue;

                    case nameof(RelayConfig.LogLevel):
                        var logLevelStr = Convert.ToString(info[i].GetValue(relayConfiguration));
                        if (int.TryParse(logLevelStr, out int iLogLevel))
                        {
                            foreach (LogLevel logLevel in Enum.GetValues(typeof(LogLevel)))
                            {
                                if ((int)logLevel == iLogLevel)
                                {
                                    continue; // Supplied log level was valid.
                                }
                            }
                        }
                        relayConfiguration.LogLevel = (LogLevel)8; // Loglevel was invalid, default to Information
                        continue;

                    case nameof(RelayConfig.MaxConnectionCount):
                        var maxConnectionCountStr = Convert.ToString(info[i].GetValue(relayConfiguration));
                        if (int.TryParse(maxConnectionCountStr, out _))
                            continue; // MCC was valid.
                        relayConfiguration.MaxConnectionCount = 25; // Connection count was invalid, default to 25.
                        continue;
                }
            }
            //Console.WriteLine("Final values after verification: ");
            //PropertyInfo[] info1 = relayConfiguration.GetType().GetProperties();
            //for (int ii = 0; ii < info.Length; ii++)
            //{
            //    Console.WriteLine($"{info1[ii].Name}: Value = {info1[ii].GetValue(relayConfiguration)} Type = {info1[ii].PropertyType}");
            //}
        }

        public static void DisplayBanner()
        {
            // https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences?redirectedfrom=MSDN
            Console.Write("\x1b[38;5;172m");
            Console.WriteLine(@"   *   '*                                                                                 ");
            Console.WriteLine(@"           *                .                                                             ");
            Console.WriteLine(@"                *           .                                                             ");
            Console.WriteLine(@"                       *    :      _____                 _            _____     _         ");
            Console.WriteLine(@"               *            !     |  |  |_ _ ___ ___ ___|_|___ ___   | __  |___| |___ _ _ ");
            Console.WriteLine(@"                     *      |     |     | | | . | -_|  _| | . |   |  |    -| -_| | .'| | |");
            Console.WriteLine(@"                            |_    |__|__|_  |  _|___|_| |_|___|_|_|  |__|__|___|_|__,|_  |");
            Console.WriteLine(@"                         ,  | `.        |___|_|                                      |___|");
            Console.WriteLine(@"               --  --- --+-<#>-+- ---  --  -                                              ");
            Console.WriteLine(@"                         `._|_,'                                                          ");
            Console.WriteLine(@"                            T                                                             ");
            Console.WriteLine(@"                            |                                                             ");
            Console.WriteLine(@"                            !                                                             ");
            Console.WriteLine(@"                            :                                                             ");
            Console.WriteLine(@"                            .                                                             ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Properly closes the server threads and disables redundant restart.
        /// </summary>
        public static void RequestServerShutdown()
        {
            HyperionRelay.ShutdownRequested = true;
            CancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Fires when Ctrl + C is pressed via the console.
        /// Cancels all threads/tasks linked to the cancellation source.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Console.WriteLine("Shutting down...");
            RequestServerShutdown();
        }


    }

}
