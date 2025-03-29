using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AudioConsolidator.Utils;
using AudioConsolidator.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AudioConsolidator.ViewModels;

public partial class ExportWindowViewModel : ViewModelBase {
	public StringWriter LogString { get; }
	public MultiStreamWriter Log { get; }

	public ExportWindowViewModel() {
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
	private bool _done;
}