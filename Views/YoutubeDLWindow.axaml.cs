using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AudioConsolidator.Views;

public partial class YoutubeDLWindow : Window {
	public YoutubeDLWindow() {
		InitializeComponent();
		LogBlock.SizeChanged += (_, _) => {
			LogScroll.ScrollToEnd();
		};
	}
}