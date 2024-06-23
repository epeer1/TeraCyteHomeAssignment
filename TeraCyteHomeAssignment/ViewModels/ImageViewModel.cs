using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MainUI.Commands;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Runtime.CompilerServices;
using Tera.UpdatedImageModel;

namespace MainUI.ViewModels
{
	public class ImageViewModel : ViewModelBase, IDisposable
	{
		private readonly ImageModel _imageModel;
		private readonly IBrightnessManager _brightnessManager;
		private readonly IHistogramManager _histogramManager;
		private readonly IImageLoader _imageLoader;
		private readonly ICommandManager _commandManager;
		private readonly ILogger _logger;

		private BitmapSource _image;
		private BitmapSource _originalImage;
		private BitmapSource _histogramImage;
		private bool _isLoading;
		private bool _isLoadingHistogram;
		private bool _enableBrightnessSlider;

		public ImageViewModel(
			ImageModel imageModel,
			IBrightnessManager brightnessManager,
			IHistogramManager histogramManager,
			IImageLoader imageLoader,
			ICommandManager commandManager,
			ILogger logger)
		{
			_imageModel = imageModel;
			_brightnessManager = brightnessManager;
			_histogramManager = histogramManager;
			_imageLoader = imageLoader;
			_commandManager = commandManager;
			_logger = logger;

			InitializeCommands();
			SubscribeToEvents();
		}

		private void InitializeCommands()
		{
			LoadImageCommand = _commandManager.Create(async () => await LoadImageAsync());
		}

		private void SubscribeToEvents()
		{
			_brightnessManager.OnBrightnessChanged += async (s, e) => await AdjustBrightness();
			_histogramManager.HistogramUpdated += (s, e) => UpdateHistogramImage();
			_imageLoader.ImageLoaded += (s, loadedImage) =>
			{
				Image = loadedImage;
				OriginalImage = loadedImage;
				EnableBrightnessSlider = true;
			};
		}

		public ICommand LoadImageCommand { get; private set; }

		public bool IsLoading
		{
			get => _isLoading;
			set => SetProperty(ref _isLoading, value);
		}

		public BitmapSource Image
		{
			get => _image;
			set => SetProperty(ref _image, value);
		}

		public BitmapSource OriginalImage
		{
			get => _originalImage;
			set => SetProperty(ref _originalImage, value);
		}

		public double Brightness
		{
			get => _brightnessManager.Brightness;
			set => _brightnessManager.Brightness = value;
		}

		public bool EnableBrightnessSlider
		{
			get => _enableBrightnessSlider;
			set => SetProperty(ref _enableBrightnessSlider, value);
		}

		public BitmapSource HistogramImage
		{
			get => _histogramImage;
			set => SetProperty(ref _histogramImage, value);
		}

		public bool IsLoadingHistogram
		{
			get => _isLoadingHistogram;
			set => SetProperty(ref _isLoadingHistogram, value);
		}

		private async Task LoadImageAsync()
		{
			try
			{
				IsLoading = true;
				EnableBrightnessSlider = false;
				await _imageLoader.LoadImageAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError("Failed to load image", ex);
				MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				IsLoading = false;
			}
		}

		private async Task AdjustBrightness()
		{
			if (OriginalImage != null)
			{
				try
				{
					EnableBrightnessSlider = false;
					using (var mat = OriginalImage.ToMat())
					{
						var adjustedMat = await _brightnessManager.AdjustBrightnessAsync(mat);
						Image = adjustedMat.ToBitmapSource();
						await Task.Run(() => _imageModel.SetMatAndUpdateHistogramAsync(adjustedMat));
					}
				}
				catch (Exception ex)
				{
					_logger.LogError("Failed to adjust brightness", ex);
					MessageBox.Show($"Error adjusting brightness: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				finally
				{
					EnableBrightnessSlider = true;
				}
			}
		}

		private void UpdateHistogramImage()
		{
			try
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					try
					{
						IsLoadingHistogram = true;
						HistogramImage = _histogramManager.GetHistogramImage();
					}
					catch (Exception ex)
					{
						_logger.LogError("Error updating histogram image on UI thread", ex);
						MessageBox.Show($"Error updating histogram: {ex.Message}", "Histogram Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					finally
					{
						IsLoadingHistogram = false;
						EnableBrightnessSlider = true;
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError("Failed to invoke histogram update on UI thread", ex);
				Application.Current.Dispatcher.Invoke(() =>
				{
					MessageBox.Show("Failed to update histogram. Please try again.", "Histogram Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
				});
			}
		}

		public void Dispose()
		{
			_imageModel.Dispose();
			_histogramManager.Dispose();
		}

		protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}
}