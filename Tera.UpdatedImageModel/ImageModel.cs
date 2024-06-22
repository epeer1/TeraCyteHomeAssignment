using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Tera.UpdatedImageModel
{
	public class ImageModel : IDisposable
	{
		public string ImagePath { get; private set; }
		private Mat _mat;
		private readonly object _lockObject = new object();
		private readonly Lazy<ImageHistogramModel> _histogramModel;

		public event EventHandler<HistogramUpdatedEventArgs> HistogramUpdated;

		public ImageModel()
		{
			_histogramModel = new Lazy<ImageHistogramModel>(() => new ImageHistogramModel());
		}

		public async Task SetMatAndUpdateHistogramAsync(Mat mat)
		{
			await Task.Run(() =>
			{
				lock (_lockObject)
				{
					_mat?.Dispose();
					_mat = mat.Clone();
				}
			});
			await UpdateHistogramAsync();
		}

		public async Task UpdateHistogramAsync()
		{
			BitmapSource histogramImage = null;
			await Task.Run(() =>
			{
				lock (_lockObject)
				{
					_histogramModel.Value.CalculateAndCreateHistogram(_mat);
					histogramImage = _histogramModel.Value.GetHistogramImageClone();
				}
			});
			OnHistogramUpdated(histogramImage);
		}

		public BitmapSource GetCloneHistogram()
		{
			lock (_lockObject)
			{
				return _histogramModel.Value.GetHistogramImageClone();
			}
		}

		protected virtual void OnHistogramUpdated(BitmapSource histogramImage)
		{
			HistogramUpdated?.Invoke(this, new HistogramUpdatedEventArgs(histogramImage));
		}

		public async Task<byte[]> GetHistogramAsByteArrayAsync()
		{
			BitmapSource histogramSource = GetCloneHistogram();
			if (histogramSource == null)
				return null;

			return await Task.Run(() =>
			{
				using (var ms = new MemoryStream())
				{
					var encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(histogramSource));
					encoder.Save(ms);
					return ms.ToArray();
				}
			});
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (_lockObject)
				{
					_mat?.Dispose();
					if (_histogramModel.IsValueCreated)
					{
						_histogramModel.Value.Dispose();
					}
				}
			}
		}

		~ImageModel()
		{
			Dispose(false);
		}
	}

	public class HistogramUpdatedEventArgs : EventArgs
	{
		public BitmapSource HistogramImage { get; }

		public HistogramUpdatedEventArgs(BitmapSource histogramImage)
		{
			HistogramImage = histogramImage;
		}
	}

	public class ImageHistogramModel : IDisposable
	{
		private const int HISTOGRAM_WIDTH = 256;
		private const int HISTOGRAM_HEIGHT = 200;
		private BitmapSource _histogramImage = null;
		private readonly object _lockObject = new object();

		public void CalculateAndCreateHistogram(Mat image)
		{
			var histograms = CalculateHistogram(image);
			CreateHistogramImage(histograms);
		}

		private (int[] Red, int[] Green, int[] Blue) CalculateHistogram(Mat image)
		{
			int[] histR = new int[256];
			int[] histG = new int[256];
			int[] histB = new int[256];

			if (image.Channels() == 1) // Grayscale
			{
				using (var hist = new Mat())
				{
					Cv2.CalcHist(new[] { image }, new[] { 0 }, null, hist, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
					hist.GetArray(out float[] tempHist);
					histR = tempHist.Select(x => (int)x).ToArray();
				}
				return (histR, histR, histR); // Return the same histogram for all channels
			}
			else // Color
			{
				Mat[] bgr = Cv2.Split(image);
				using (var histMatR = new Mat())
				using (var histMatG = new Mat())
				using (var histMatB = new Mat())
				{
					Cv2.CalcHist(new[] { bgr[2] }, new[] { 0 }, null, histMatR, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
					Cv2.CalcHist(new[] { bgr[1] }, new[] { 0 }, null, histMatG, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
					Cv2.CalcHist(new[] { bgr[0] }, new[] { 0 }, null, histMatB, 1, new[] { 256 }, new[] { new Rangef(0, 256) });

					histMatR.GetArray(out float[] tempHistR);
					histMatG.GetArray(out float[] tempHistG);
					histMatB.GetArray(out float[] tempHistB);

					histR = tempHistR.Select(x => (int)x).ToArray();
					histG = tempHistG.Select(x => (int)x).ToArray();
					histB = tempHistB.Select(x => (int)x).ToArray();
				}
				return (histR, histG, histB);
			}
		}

		private void CreateHistogramImage((int[] Red, int[] Green, int[] Blue) histograms)
		{
			using (var histImage = new Mat(HISTOGRAM_HEIGHT, HISTOGRAM_WIDTH, MatType.CV_8UC3, Scalar.White))
			{
				DrawHistogram(histImage, histograms.Red, new Scalar(0, 0, 255));   // Red
				DrawHistogram(histImage, histograms.Green, new Scalar(0, 255, 0)); // Green
				DrawHistogram(histImage, histograms.Blue, new Scalar(255, 0, 0));  // Blue

				lock (_lockObject)
				{
					_histogramImage = histImage.ToBitmapSource();
					_histogramImage.Freeze(); // Make it immutable for thread-safety
				}
			}
		}

		private void DrawHistogram(Mat histImage, int[] histogram, Scalar color)
		{
			int max = histogram.Max();
			for (int i = 0; i < histogram.Length; i++)
			{
				int height = (int)((histogram[i] / (double)max) * HISTOGRAM_HEIGHT);
				Cv2.Line(
					histImage,
					new Point(i, HISTOGRAM_HEIGHT),
					new Point(i, HISTOGRAM_HEIGHT - height),
					color
				);
			}
		}

		public BitmapSource GetHistogramImageClone()
		{
			lock (_lockObject)
			{
				return _histogramImage?.Clone();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (_lockObject)
				{
					_histogramImage = null;
				}
			}
		}

		~ImageHistogramModel()
		{
			Dispose(false);
		}
	}
}