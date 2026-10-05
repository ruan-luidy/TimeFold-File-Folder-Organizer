using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using TimeFold.Core.Config;
using TimeFold.Wpf.Features.Exclusions;
using TimeFold.Wpf.Features.FileTypes;
using TimeFold.Wpf.Features.Modes;
using TimeFold.Wpf.Features.Naming;
using TimeFold.Wpf.Features.Organize;
using TimeFold.Wpf.Features.Preview;
using TimeFold.Wpf.Features.Settings;
using TimeFold.Wpf.Features.Source;
using TimeFold.Wpf.Features.Undo;
using TimeFold.Wpf.Features.Updates;
using TimeFold.Wpf.Shared.Progress;
using TimeFold.Wpf.Shared.Services;
using TimeFold.Wpf.Shell;

namespace TimeFold.Wpf
{
    public partial class App : Application
    {
        private ServiceProvider? _services;
        private bool _showingError;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            _services = ConfigureServices();
            var state = _services.GetRequiredService<AppState>();
            ThemeService.Apply(state.Settings.DarkMode);

            // args[0] may be a folder from the Explorer context menu (%1 / %V)
            string? initialFolder = e.Args.Length > 0 && Directory.Exists(e.Args[0]) ? e.Args[0] : null;

            var window = _services.GetRequiredService<MainWindow>();
            var viewModel = _services.GetRequiredService<MainViewModel>();
            MainWindow = window;
            window.Loaded += async (_, _) => await viewModel.StartAsync(initialFolder);
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _services?.Dispose();
            base.OnExit(e);
        }

        // One window, so every view model is a singleton
        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<AppState>();
            services.AddSingleton<StatusService>();
            services.AddSingleton<ProgressViewModel>();
            services.AddSingleton<UpdateChecker>();

            services.AddSingleton<PreviewViewModel>();
            services.AddSingleton<SourceViewModel>();
            services.AddSingleton<ModesViewModel>();
            services.AddSingleton<UndoViewModel>();
            services.AddSingleton<OrganizeViewModel>();
            services.AddSingleton<NamingViewModel>();
            services.AddSingleton<FileTypesViewModel>();
            services.AddSingleton<ExclusionsViewModel>();
            services.AddSingleton<SettingsViewModel>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        // An error that escapes a handler goes to errors.log and becomes a dialog instead of closing the app
        private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var details = e.Exception.ToString();
            try
            {
                string dir = AppConstants.GetConfigDirectoryPath();
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "errors.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {details}{Environment.NewLine}{Environment.NewLine}");
            }
            catch (IOException)
            {
            }

            if (_showingError)
                return;

            _showingError = true;
            try
            {
                await DialogService.ErrorAsync("Something went wrong", e.Exception.GetBaseException().Message, details);
            }
            finally
            {
                _showingError = false;
            }
        }
    }
}
