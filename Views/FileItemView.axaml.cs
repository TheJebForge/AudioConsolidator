using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AudioConsolidator.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace AudioConsolidator.Views;

public partial class FileItemView : UserControl {
	public FileItemView() {
		InitializeComponent();
		Draggable.PointerPressed += DraggableOnPointerPressed;
	}

	private async void DraggableOnPointerPressed(object? sender, PointerPressedEventArgs e) {
		try {
			if (DataContext is not FileItemViewModel viewModel) return;
			if (TopLevel.GetTopLevel(this) is not { } topLevel) return;
			
			e.Handled = true;

			if (viewModel.ParentList is not { } parentList) return;

			MainWindowViewModel.DraggedFrom = parentList;
			
			if (!viewModel.Selected) {
				foreach (var item in parentList) {
					item.Selected = item == viewModel;
				}
			}

			var files = await GetFiles().ToArrayAsync();
			
			var data = new DataObject();
			data.Set(DataFormats.Files, files);
		
			await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
			MainWindowViewModel.DraggedFrom = null;
			
			async IAsyncEnumerable<IStorageFile> GetFiles() {
				foreach (var item in parentList) {
					if (!item.Selected) continue;
					if (item.Source is not { } source) continue;
					if (await topLevel.StorageProvider.TryGetFileFromPathAsync(source) is not { } file) continue;
					yield return file;
				}
			}
		} catch (Exception exception) {
			Console.Error.WriteLine(exception);
		}
	}
}