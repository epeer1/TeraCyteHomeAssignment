using System.Net.Http;
using System.Threading.Tasks;
using Tera.UpdatedImageModel;


namespace Tera.NetworkServices
{
	public class HistogramSender : IDisposable
	{
		private readonly string _azureFunctionUrl;
		private readonly HttpClient _httpClient;
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10, 10);	//Manage the requests to the cloud
		private const int ThrottleMilliseconds = 200;

		public HistogramSender(string azureFunctionUrl)		//Will use publish subscribe for the real big data
		{
			_azureFunctionUrl = azureFunctionUrl;
			_httpClient = new HttpClient
				              {
					              Timeout = TimeSpan.FromSeconds(10)
				              };
		}

		public async Task SendHistogramAsync(ImageModel imageModel)
		{
			//if (await _semaphore.WaitAsync(0))
			//{
			//	try
			//	{
			//		byte[] histogramData = imageModel.GetHistogramAsByteArray();
			//		if (histogramData == null)
			//			return;

			//		using (var content = new ByteArrayContent(histogramData))
			//		{
			//			content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
			//			var response = await _httpClient.PostAsync(_azureFunctionUrl, content);

			//			if (!response.IsSuccessStatusCode)
			//			{
			//				// Handle error, maybe throw an exception or log the error
			//				//throw new HttpRequestException($"Error sending histogram: {response.StatusCode}");
			//				Console.WriteLine("NO CONNECTION YET!");
			//			}
			//		}

			//		// Throttle
			//		await Task.Delay(ThrottleMilliseconds);
			//	}
			//	finally
			//	{
			//		_semaphore.Release();
			//	}
			//}
			//else
			//{
			//	// Optionally log that a send operation was skipped due to throttling
			//	Console.WriteLine("Histogram send operation skipped due to throttling");
			//}
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
			_semaphore.Dispose();
		}
	}
}
