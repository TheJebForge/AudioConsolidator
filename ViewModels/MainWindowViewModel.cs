using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AudioConsolidator.Views;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFMpegCore;
using FFMpegCore.Arguments;

namespace AudioConsolidator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
	public partial class FileGroup : ObservableObject {
		public FileGroup(string name) {
			_name = name;
		}

		public FileGroup(IEnumerable<FileItemViewModel> initial, string name) {
			_name = name;
			_files = new(initial);
		}

		[ObservableProperty]
		private string _name;

		[ObservableProperty]
		private ObservableCollection<FileItemViewModel> _files = [];

		[RelayCommand]
		private void SelectAll() {
			foreach (var file in Files) {
				file.Selected = true;
			}
		}

		[RelayCommand]
		private void DeselectAll() {
			foreach (var file in Files) {
				file.Selected = false;
			}
		}
	}

	public static ObservableCollection<FileItemViewModel>? DraggedFrom { get; set; }

	[ObservableProperty]
	private ObservableCollection<FileItemViewModel> _availableFiles = [];

	[ObservableProperty]
	private ObservableCollection<FileGroup> _groups = [];

	[ObservableProperty]
	private bool _binariesReady;

	[ObservableProperty]
	private string _outputPath = "out/";

	[ObservableProperty]
	private int _bitrate = 320;

	public void AddFile(IStorageItem file) {
		AvailableFiles.Add(new(file, AvailableFiles));
	}

	[RelayCommand]
	private void AddGroup() {
		Groups.Add(new($"Side{Groups.Count + 1}"));
	}

	[RelayCommand]
	private void RemoveGroup(FileGroup group) {
		Groups.Remove(group);
	}

	[RelayCommand]
	private void SelectAll() {
		foreach (var file in AvailableFiles) {
			file.Selected = true;
		}
	}

	[RelayCommand]
	private void DeselectAll() {
		foreach (var file in AvailableFiles) {
			file.Selected = false;
		}
	}

	[RelayCommand]
	private async Task BrowseOutput() {
		var folders = await App.MainWindow.StorageProvider
			.OpenFolderPickerAsync(new() { AllowMultiple = false });

		if (folders.Count <= 0) return;

		OutputPath = WebUtility.UrlDecode(folders[0].Path.AbsolutePath);
	}

	private YoutubeDLWindow? _dlWindow;
	
	[RelayCommand]
	private void OpenYoutubeDownloader() {
		if (_dlWindow is not null) return;

		var dlView = new YoutubeDLViewModel();
		_dlWindow = new() {
			DataContext = dlView
		};
		_dlWindow.Show(App.MainWindow);
		_dlWindow.Closed += (_, _) => {
			_dlWindow = null;
			dlView.Cancel.Cancel();
		};
	}

	[RelayCommand]
	private async Task AddDownloaded() {
		var files = await App.MainWindow.StorageProvider
			.OpenFilePickerAsync(new() {
				AllowMultiple = true,
				SuggestedStartLocation = await App.MainWindow.StorageProvider
					.TryGetFolderFromPathAsync(App.DownloadedPath)
			});

		foreach (var file in files) {
			AddFile(file);
		}
	}

	[RelayCommand]
	private void Export() {
		var exportView = new ExportWindowViewModel();
		var exportWindow = new ExportWindow {
			DataContext = exportView
		};

		exportWindow.ShowDialog(App.MainWindow);

		Task.Run(
			() => {
				try {
					Directory.CreateDirectory(OutputPath);
					
					foreach (var group in Groups) {
						var files = group.Files.Where(i => i.Source is not null)
							.Select(i => WebUtility.UrlDecode(i.Source!.AbsolutePath))
							.ToArray();

						var consolidatedOutputPath = Path.Join(App.TempPath, $"{group.Name}.wav");
						var outputPath = Path.Join(OutputPath, $"{group.Name}.ogg");

						UI(
							() => exportView.WriteLine(
								$"Consolidating files from '{group.Name}' into '{consolidatedOutputPath}'"
							)
						);

						if (!FFMpegArguments.FromConcatInput(files)
							    .OutputToFile(
								    consolidatedOutputPath,
								    addArguments: options => options.WithoutMetadata()
									    .WithArgument(new CustomArgument("-vn"))
							    )
							    .ProcessSynchronously()) {
							Console.Error.WriteLine("Failed to process files");
							return;
						}

						UI(() => exportView.WriteLine($"Compressing '{consolidatedOutputPath}' to '{outputPath}'"));

						if (!FFMpegArguments.FromFileInput(consolidatedOutputPath)
							    .OutputToFile(
								    outputPath,
								    addArguments: options => options
									    .WithArgument(new CustomArgument("-vn"))
									    .WithAudioCodec("libvorbis")
									    .WithAudioBitrate(Bitrate)
							    )
							    .ProcessSynchronously()) {
							Console.Error.WriteLine("Failed to process files");
							return;
						}

						UI(() => exportView.WriteLine($"Deleting '{consolidatedOutputPath}'"));
						File.Delete(consolidatedOutputPath);
					}

					UI(
						() => {
							exportView.WriteLine("Successfully finished consolidating files");
							exportView.Done = true;
						}
					);
				} catch (Exception e) {
					UI(
						() => {
							exportView.WriteLine(e);
							exportView.Done = true;
						}
					);
				}

				return;

				void UI(Action action) {
					Dispatcher.UIThread.Invoke(action);
				}
			}
		);
	}
}