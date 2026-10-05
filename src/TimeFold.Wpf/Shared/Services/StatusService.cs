using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeFold.Wpf.Shared.Services
{
    // Short notice in the status bar ("Path copied"); it clears itself after a few seconds
    public sealed partial class StatusService : ObservableObject
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

        [ObservableProperty]
        private string _message = string.Empty;

        public StatusService()
        {
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                Message = string.Empty;
            };
        }

        public void Show(string message)
        {
            Message = message;
            _timer.Stop();
            _timer.Start();
        }
    }
}
