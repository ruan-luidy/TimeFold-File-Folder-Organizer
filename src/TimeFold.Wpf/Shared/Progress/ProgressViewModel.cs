using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Shared.Progress
{
    /// <summary>
    /// The progress card shown while files move (organize and undo share it): bar, counter, the live log
    /// when "Show detailed progress" is on, and Cancel.
    /// </summary>
    public sealed partial class ProgressViewModel : ObservableObject
    {
        private readonly AppState _state;
        private CancellationTokenSource? _cts;
        private string _verb = "Processing";
        private string _logPrefix = "✓";
        private string _cancelPrompt = string.Empty;

        [ObservableProperty]
        private bool _isActive;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private int _current;

        [ObservableProperty]
        private int _total = 1;

        [ObservableProperty]
        private string _progressText = "Initializing...";

        public ProgressViewModel(AppState state)
        {
            _state = state;
        }

        public ObservableCollection<string> LogLines { get; } = new();

        public bool ShowLog => _state.Settings.ShowDetailedProgress;

        public (IProgress<(int current, int total, string currentFile)> Progress, CancellationToken Token) Begin(string title, string verb, string logPrefix, string cancelPrompt, params string[] header)
        {
            Title = title;
            _verb = verb;
            _logPrefix = logPrefix;
            _cancelPrompt = cancelPrompt;
            Current = 0;
            Total = 1;
            ProgressText = "Initializing...";
            LogLines.Clear();
            foreach (var line in header) LogLines.Add(line);
            OnPropertyChanged(nameof(ShowLog));

            _cts = new CancellationTokenSource();
            IsActive = true;

            var progress = new Progress<(int current, int total, string currentFile)>(p =>
            {
                Total = Math.Max(1, p.total);
                Current = p.current;
                ProgressText = $"{_verb}: {p.current} of {p.total} files";
                if (ShowLog) LogLines.Add($"{_logPrefix} {p.currentFile}");
            });
            return (progress, _cts.Token);
        }

        public void End()
        {
            IsActive = false;
            _cts?.Dispose();
            _cts = null;
        }

        [RelayCommand]
        private async Task Cancel()
        {
            if (_cts == null) return;
            if (await DialogService.ConfirmAsync("Cancel", _cancelPrompt, "Cancel operation", "Keep going"))
                _cts?.Cancel();
        }
    }
}
