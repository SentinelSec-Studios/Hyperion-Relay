using System.Net.NetworkInformation;

namespace HyperionRelay.Utilities
{
    internal class LatencyUtility
    {
        public static long PingAddress(string address)
        {
            Ping ping = new();
            PingOptions pingOptions = new();

            pingOptions.DontFragment = true;
            var payload = new byte[32];
            for (int byteIndice = 0; byteIndice < payload.Length; ++byteIndice)
            {
                payload[byteIndice] = (byte)byteIndice;
            }

            int timeout = 120;
            PingReply reply = ping.Send(address, timeout, payload, pingOptions);
            if (reply.Status == IPStatus.Success)
            {
                return reply.RoundtripTime;
            }
            return -1;
        }
    }

    /// Thie code was originally written by Arshman Saleem on stack overflow, further modifications have been made.
    internal class ConsoleUtility
    {
        private const char _block = '■';
        private const string _back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
        private const string _twirl = "-\\|/";

        public static void WriteProgressBar(int percent, bool isUpdate = false)
        {
            if (isUpdate)
                Console.Write(_back);
            Console.Write("[");
            var p = (int)((percent / 10f) + .5f);
            for (var i = 0; i < 10; ++i)
            {
                if (i >= p)
                    Console.Write(' ');
                else
                    Console.Write(_block);
            }
            Console.Write("] {0,3:##0}%", percent);
        }

        private static void WriteProgressSpinner(int progress, bool isUpdate = false, bool isDone = false)
        {
            if (isDone)
            {
                Console.WriteLine("\b[Done]");
                return;
            }
            if (isUpdate)
                Console.Write("\b");
            Console.Write(_twirl[progress % _twirl.Length]);
        }

        private static CancellationTokenSource cts = new();
        private static CancellationToken cancellationToken;
        private static bool isRunning = false;

        public static async void StartProgressSpinner()
        {
            if (isRunning)
                throw new InvalidOperationException("The spinner is already running!");
            if (cts.IsCancellationRequested)
                cts = new CancellationTokenSource();
            cancellationToken = cts.Token;
            isRunning = true;
            try
            {
                await Task.Run(async () =>
                {
                    int i = 0;
                    ConsoleUtility.WriteProgressSpinner(0); //Initialize a progress spinner without updating.
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        ConsoleUtility.WriteProgressSpinner(i, true);
                        ++i;
                        await Task.Delay(50);
                        // to prevent long operations from overflowing the integer, we set the value to zero at 100.
                        if (i == 100)
                            i = 0;
                    }
                }, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        public static void StopProgressSpinner()
        {
            if (!isRunning)
                throw new InvalidOperationException("The spinner is not running!");
            cts.Cancel();
            ConsoleUtility.WriteProgressSpinner(100, true, true);
            isRunning = false;
        }

    }
}
