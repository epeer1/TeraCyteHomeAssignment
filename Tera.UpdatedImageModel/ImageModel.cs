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
		private readonly ImageHistogramModel _histogramModel;

		public event EventHandler HistogramUpdated;
		public event EventHandler ImageLoaded;

		public event EventHandler HistogramReadyToSend;

		public ImageModel()
		{
			_histogramModel = new ImageHistogramModel();
		}

		public void SetMatAndHistogram(Mat mat)
		{
			_mat?.Dispose();
			_mat = mat.Clone();
			ImageLoaded?.Invoke(this,null);	//Invoke the UI thread and then get the Image clone

			Task.Run(() => UpdateHistogram(mat));	//Go behind to update histogram
		}

		public void UpdateHistogram(Mat mat)
		{
			_histogramModel.CalculateAndCreateHistogram(mat);
			var histogramImage = _histogramModel.GetHistogramImageClone();
			HistogramUpdated?.Invoke(this,null); //Invoke the UI thread and then get the histogram clone

			_histogramModel.InitializeHistogramByteArray();
			HistogramReadyToSend?.Invoke(this,null); //STOP HERE
		}

		public BitmapSource GetCloneHistogram()
		{
				return _histogramModel.GetHistogramImageClone();
		}

		public BitmapSource GetImageClone()
		{
			return _mat.ToBitmapSource().Clone();
		}

		public byte[] GetHisotgramByteArray()
		{
			return _histogramModel.GetHistogramBytesArray();
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
					_histogramModel?.Dispose();
				}
			}
		}

		~ImageModel()
		{
			Dispose(false);
		}
	}

	public class ImageHistogramModel : IDisposable
	{
		private const int HISTOGRAM_WIDTH = 256;
		private const int HISTOGRAM_HEIGHT = 200;
		private BitmapSource _histogramImage = null;
		private byte[] _histogramByteArray;
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

		public void InitializeHistogramByteArray()
		{
			lock (_lockObject)
			{
				if (_histogramImage == null)
				{
					_histogramByteArray = null;
					return;
				}

				using (var memoryStream = new MemoryStream())
				{
					BitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(_histogramImage));
					encoder.Save(memoryStream);
					_histogramByteArray = memoryStream.ToArray();
				}
			}
		}

		public BitmapSource GetHistogramImageClone()
		{
			lock (_lockObject)
			{
				return _histogramImage?.Clone();
			}
		}

		public byte[] GetHistogramBytesArray()
		{
			return _histogramByteArray;
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