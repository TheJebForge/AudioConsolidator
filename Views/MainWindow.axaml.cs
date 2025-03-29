using System;
using AudioConsolidator.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AudioConsolidator.Views;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();

		CmdButton.Click += ShowCmdLog;
		
		AddHandler(DragDrop.DragOverEvent, DragOverEvent);
		AddHandler(DragDrop.DragLeaveEvent, DragLeaveEvent);
		AddHandler(DragDrop.DropEvent, DropEvent);
	}

	private void DragOverEvent(object? sender, DragEventArgs e) {
		if (!e.Data.Contains(DataFormats.Files)) {
			e.DragEffects = DragDropEffects.None;
			return;
		}

		if (e.Source is not Border border || !border.Classes.Contains("DropTarget")) return;
		
		e.DragEffects = DragDropEffects.Copy;
		border.BorderBrush = Brushes.GreenYellow;
	}
	
	private void DragLeaveEvent(object? sender, DragEventArgs e) {
		if (e.Source is not Border border || !border.Classes.Contains("DropTarget")) return;
		
		border.BorderBrush = Brushes.Transparent;
	}
	
	private void DropEvent(object? sender, DragEventArgs e) {
		if (e.Source is not Border border || !border.Classes.Contains("DropTarget")) return;
		border.BorderBrush = Brushes.Transparent;

		if (e.Data.GetFiles() is not { } files) return;

		var list = border.DataContext switch {
			MainWindowViewModel mainWindow => mainWindow.AvailableFiles,
			MainWindowViewModel.FileGroup group => group.Files,
			_ => throw new ApplicationException("Unexpected drop target!")
		};

		if (list == MainWindowViewModel.DraggedFrom) return;

		foreach (var file in files) {
			if (MainWindowViewModel.DraggedFrom is { } draggedFrom) {
				for (var i = draggedFrom.Count - 1; i >= 0; i--) {
					if (draggedFrom[i].Source == file.Path) {
						draggedFrom.RemoveAt(i);
					}
				}
			}
			
			list.Add(new(file, list));
		}
	}

	private CmdLogWindow? _cmdLogWindow;
	
	public void ShowCmdLog(object? sender, RoutedEventArgs routedEventArgs) {
		if (_cmdLogWindow is not null) {
			_cmdLogWindow.Focus();
			return;
		}
		
		var cmdView = new CmdLogViewModel();
		
		_cmdLogWindow = new() {
			DataContext = cmdView
		};

		_cmdLogWindow.Show(this);
		_cmdLogWindow.Closed += (_, _) => {
			cmdView.Dispose();
			_cmdLogWindow = null;
		};
	}
}