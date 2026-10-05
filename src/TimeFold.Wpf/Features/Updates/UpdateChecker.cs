using TimeFold.Core.Config;
using TimeFold.Core.Updates;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Updates
{
    // The update check of MainForm.Events: silent on startup when offline, a message when asked by hand
    public sealed class UpdateChecker
    {
        private readonly AppState _state;

        public UpdateChecker(AppState state)
        {
            _state = state;
        }

        public async Task CheckOnStartupAsync(int delayMs)
        {
            try
            {
                await Task.Delay(delayMs);
                var result = await UpdateService.CheckAsync();
                // offline or API error: stay silent on startup, never show a false "up to date"
                if (!result.Success || result.Info == null) return;

                if (!string.IsNullOrEmpty(_state.Settings.SkippedUpdateVersion) &&
                    !UpdateService.IsNewer(result.Info.Version, _state.Settings.SkippedUpdateVersion))
                    return;

                await ShowAsync(result.Info);
            }
            catch { /* never surface update errors to the user */ }
        }

        public async Task CheckManuallyAsync()
        {
            var result = await UpdateService.CheckAsync();
            if (!result.Success)
            {
                await DialogService.WarningAsync("Update Check Failed", "Could not reach the update server. Please check your internet connection and try again.");
                return;
            }
            if (result.Info == null)
            {
                await DialogService.SuccessAsync("No Updates Available", $"TimeFold {AppConstants.AppVersion} is the latest version. You're up to date!");
                return;
            }
            await ShowAsync(result.Info);
        }

        private async Task ShowAsync(UpdateService.UpdateInfo info)
        {
            var action = await DialogService.ShowAsync<UpdateAction>(new UpdateDialog(info));
            if (action == UpdateAction.Skip)
            {
                _state.Settings.SkippedUpdateVersion = info.Version;
                _state.Settings.SaveToFile();
            }
        }
    }
}
