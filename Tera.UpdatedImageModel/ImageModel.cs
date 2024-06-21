using System;
using OpenCvSharp;

namespace Tera.UpdatedImageModel
{
	public class ImageModel : IDisposable
	{
		public string ImagePath { get; private set; }
		private Mat _mat;

		public void LoadImage(string path)
		{
			ImagePath = path;
			_mat = Cv2.ImRead(path, ImreadModes.Color);
		}

		public Mat GetMat()
		{
			return _mat.Clone();
		}

		public void Dispose()
		{
			_mat?.Dispose();
		}
	}
}