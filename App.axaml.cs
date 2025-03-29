using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioConsolidator.Utils;
using Avalonia.Markup.Xaml;
using AudioConsolidator.ViewModels;
using AudioConsolidator.Views;
using FFMpegCore;
using YoutubeDLSharp;

namespace AudioConsolidator;

public partial class App : Application {
	public override void Initialize() {
		AvaloniaXamlLoader.Load(this);
	}

	public const string BinPath = "bin/";
	public const string TempPath = "temp/";
	public const string DownloadedPath = "downloaded/";
		
	public static YoutubeDL? YoutubeDl { get; private set; }
	public static StringWriter OutLog { get; } = new();
	
	public static MainWindow MainWindow { get; private set; } = null!;
	public static MainWindowViewModel MainViewModel { get; private set; } = null!;

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			// Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
			// More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
			DisableAvaloniaDataAnnotationValidation();

			MainViewModel = new();
			MainWindow = new() {
				DataContext = MainViewModel
			};
			desktop.MainWindow = MainWindow;
			
			Console.SetOut(new MultiStreamWriter(Console.Out, OutLog));
			Console.SetError(new MultiStreamWriter(Console.Error, OutLog));
			
			Task.Run(
				async () => {
					Directory.CreateDirectory(BinPath);
					Directory.CreateDirectory(TempPath);
					Directory.CreateDirectory(DownloadedPath);
					
					Console.WriteLine("Checking for binaries and downloading missing ones...");
					
					await YoutubeDLSharp.Utils.DownloadBinaries(true, BinPath);
					
					YoutubeDl = new() {
						OutputFolder = DownloadedPath
					};
					Console.WriteLine(await YoutubeDl.RunUpdate());
					GlobalFFOptions.Configure(
						options => {
							options.BinaryFolder = BinPath;
							options.TemporaryFilesFolder = TempPath;
						});
					
					Console.WriteLine("Everything is configured!");
					MainViewModel.BinariesReady = true;
				}
			);
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void DisableAvaloniaDataAnnotationValidation() {
		// Get an array of plugins to remove
		var dataValidationPluginsToRemove =
			BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

		// remove each entry found
		foreach (var plugin in dataValidationPluginsToRemove) {
			BindingPlugins.DataValidators.Remove(plugin);
		}
	}
}