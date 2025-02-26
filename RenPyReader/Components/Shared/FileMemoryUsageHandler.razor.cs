using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace RenPyReader.Components.Shared
{
    public partial class FileMemoryUsageHandler
    {
        private Timer? timer;

        private long memoryUsage;

        public event Action<long> MemoryUsageUpdated = delegate { };

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                timer = new Timer(1000);
                timer.Elapsed += OnTimerElapsed;
            }
        }

        public void Start()
        {
            timer?.Start();
        }

        public void Stop()
        {
            timer?.Stop();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            memoryUsage = Process.GetCurrentProcess().PrivateMemorySize64;
            MemoryUsageUpdated?.Invoke(memoryUsage);
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            timer?.Stop();
            timer?.Dispose();
        }
    }
}