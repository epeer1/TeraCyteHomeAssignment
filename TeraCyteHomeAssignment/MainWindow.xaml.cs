using System.Windows;
using System.Windows.Media.Imaging;
using MainUI.ViewModels;

namespace MainUI
{
	public partial class MainWindow : Window
	{
		private const double ZoomFactor = 1.2;
		private const double MinZoom = 0.1;
		private const double MaxZoom = 10;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainViewModel();
		}

		private void ZoomIn_Click(object sender, RoutedEventArgs e)
		{
			ApplyZoom(ZoomFactor);
		}

		private void ZoomOut_Click(object sender, RoutedEventArgs e)
		{
			ApplyZoom(1 / ZoomFactor);
		}

		private void ResetZoom_Click(object sender, RoutedEventArgs e)
		{
			ImageScale.ScaleX = 1;
			ImageScale.ScaleY = 1;
		}

		private void ApplyZoom(double factor)
		{
			double newZoom = ImageScale.ScaleX * factor;
			if (newZoom >= MinZoom && newZoom <= MaxZoom)
			{
				ImageScale.ScaleX *= factor;
				ImageScale.ScaleY *= factor;
			}
		}

		private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (DataContext is MainViewModel mainViewModel)
			{
				mainViewModel.ImageViewModel.Brightness = e.NewValue;
			}
		}

		private void IncreaseBrightness_Click(object sender, RoutedEventArgs e)
		{
			if (BrightnessSlider.Value < BrightnessSlider.Maximum)
			{
				BrightnessSlider.Value += 0.1;
			}
		}

		private void DecreaseBrightness_Click(object sender, RoutedEventArgs e)
		{
			if (BrightnessSlider.Value > BrightnessSlider.Minimum)
			{
				BrightnessSlider.Value -= 0.1;
			}
		}


	}
}