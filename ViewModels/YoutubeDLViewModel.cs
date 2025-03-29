using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioConsolidator.Utils;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace AudioConsolidator.ViewModels;

public partial class YoutubeDLViewModel : ViewModelBase {
	[ObservableProperty]
	private bool _downloadIndividualSongs = true;

	[ObservableProperty]
	private bool _downloadPlaylist;

	[ObservableProperty]
	private string _links = "";

	[ObservableProperty]
	private string _playlistLink = "";
	
	public StringWriter LogString { get; }
	public MultiStreamWriter Log { get; }

	public CancellationTokenSource Cancel { get; } = new();

	public YoutubeDLViewModel() {
		LogString = new();
		Log = new(Console.Out, LogString);
	}

	public void WriteLine(string line) {
		Log.WriteLine(line);
		OnPropertyChanged(nameof(LogContents));
	}

	public void WriteLine(string format, params object[] args) {
		Log.WriteLine(format, args);
		OnPropertyChanged(nameof(LogContents));
	}

	public void WriteLine(object value) {
		Log.WriteLine(value);
		OnPropertyChanged(nameof(LogContents));
	}

	public string LogContents => LogString.ToString();

	[ObservableProperty]
	private float _progress;

	[ObservableProperty]
	private string _progressLabel = "Not started";

	[ObservableProperty]
	private bool _downloading;

	[RelayCommand]
	private async Task Download() {
		try {
			var yt = App.YoutubeDl!;
			Downloading = true;
			
			if (DownloadIndividualSongs) {
				var links = Links.Split('\n');

				var progressSlice = 100f / links.Length;
				
				WriteLine("Starting download...");
				foreach (var (index, link) in links.Index()) {
					var trimmed = link.Trim();
					var offset = progressSlice * index;

					if (Cancel.IsCancellationRequested) return;
					
					WriteLine($"Downloading '{trimmed}'");
					
					var result = await yt.RunAudioDownload(
						trimmed, 
						AudioConversionFormat.Vorbis, 
						ct: Cancel.Token,
						progress: new Progress<DownloadProgress>(
							progress => {
								var currentProgress = offset + progress.Progress * progressSlice;

								Progress = currentProgress;
								ProgressLabel = $"Downloading {index + 1} of {links.Length}: {progress.DownloadSpeed} {progress.TotalDownloadSize}";
							}
						)
					);
					
					if (await App.MainWindow.StorageProvider.TryGetFileFromPathAsync(result.Data) is not { } file) continue;
					
					WriteLine($"Downloaded and added '{result.Data}'");
					
					App.MainViewModel.AddFile(file);
				}
			} else if (DownloadPlaylist) {
				WriteLine("Starting download...");
				var result = await yt.RunAudioPlaylistDownload(
					PlaylistLink,
					format: AudioConversionFormat.Vorbis,
					ct: Cancel.Token,
					progress: new Progress<DownloadProgress>(
						progress => {
							Progress = progress.Progress * 100;
							ProgressLabel = $"Downloading {progress.Progress:P}: {progress.DownloadSpeed} {progress.TotalDownloadSize}";
						}
					),
					output: new Progress<string>(
						progress => {
							if (!progress.StartsWith("outfile")) return;
							var split = progress.Split("outfile: ", 2);
							if (split.Length < 2) return;
							WriteLine($"Downloaded {split[1]}");
						}
					)
				);

				foreach (var path in result.Data) {
					if (await App.MainWindow.StorageProvider.TryGetFileFromPathAsync(path) is not { } file) continue;
					
					WriteLine($"Added '{path}'");
					
					App.MainViewModel.AddFile(file);
				}
			}
			
			WriteLine("Finished downloading!");
				
			Progress = 100f;
			ProgressLabel = "Done";
		} catch (Exception e) {
			WriteLine(e);
		}

		Downloading = false;
	}
}