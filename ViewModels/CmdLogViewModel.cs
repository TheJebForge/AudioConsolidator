using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace AudioConsolidator.ViewModels;

public class CmdLogViewModel : ViewModelBase, IDisposable {
	private readonly CancellationTokenSource _cancellation;
	
	public CmdLogViewModel() {
		_cancellation = new();
		Task.Run(
			async () => {
				while (!_cancellation.Token.IsCancellationRequested) {
					Dispatcher.UIThread.Invoke(() => OnPropertyChanged(nameof(Log)));
					await Task.Delay(1000, _cancellation.Token);
				}
			}
		);
	}

	public void Dispose() {
		_cancellation.Cancel();
		_cancellation.Dispose();
		GC.SuppressFinalize(this);
	}

	public string Log => App.OutLog.ToString();
}