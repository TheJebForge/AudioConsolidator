using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AudioConsolidator.ViewModels;

public partial class FileItemViewModel(
	string name, 
	Uri? source, 
	ObservableCollection<FileItemViewModel>? parentList
) : ViewModelBase {
	[ObservableProperty]
	private string _name = name;

	[ObservableProperty]
	private ObservableCollection<FileItemViewModel>? _parentList = parentList;

	[ObservableProperty]
	private bool _selected;

	[ObservableProperty]
	private Uri? _source = source;

	public FileItemViewModel() : this("", null, null) { }

	public FileItemViewModel(IStorageItem file, ObservableCollection<FileItemViewModel>? parentList) : this(
		file.Name,
		file.Path,
		parentList
	) {}

	[RelayCommand]
	private void MoveUp() {
		if (ParentList is null) return;

		if (Selected) {
			for (var i = 0; i < ParentList.Count; i++) {
				if (!ParentList[i].Selected) continue;
				if (i <= 0) return;

				(ParentList[i], ParentList[i - 1]) = (ParentList[i - 1], ParentList[i]);
			}
		} else {
			for (var i = 0; i < ParentList.Count; i++) {
				if (ParentList[i] != this) continue;
				if (i <= 0) return;
			
				ParentList[i] = ParentList[i - 1];
				ParentList[i - 1] = this;
				return;
			}
		}
	}
	
	[RelayCommand]
	private void MoveDown() {
		if (ParentList is null) return;

		if (Selected) {
			for (var i = ParentList.Count - 1; i >= 0; i--) {
				if (!ParentList[i].Selected) continue;
				if (i >= ParentList.Count - 1) return;

				(ParentList[i], ParentList[i + 1]) = (ParentList[i + 1], ParentList[i]);
			}
		} else {
			for (var i = 0; i < ParentList.Count; i++) {
				if (ParentList[i] != this) continue;
				if (i >= ParentList.Count - 1) return;
			
				ParentList[i] = ParentList[i + 1];
				ParentList[i + 1] = this;
				return;
			}
		}
	}

	[RelayCommand]
	private void Remove() {
		if (ParentList is null) return;

		if (Selected) {
			for (var i = ParentList.Count - 1; i >= 0; i--) {
				if (!ParentList[i].Selected) continue;

				ParentList.RemoveAt(i);
			}
		} else {
			for (var i = 0; i < ParentList.Count; i++) {
				if (ParentList[i] != this) continue;

				ParentList.RemoveAt(i);
				return;
			}
		}
	}
}