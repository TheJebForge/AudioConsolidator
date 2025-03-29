using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AudioConsolidator.Views;

public partial class ExportWindow : Window {
	public ExportWindow() {
		InitializeComponent();
		LogBlock.SizeChanged += (_, _) => {
			LogScroll.ScrollToEnd();
		};
	}
}