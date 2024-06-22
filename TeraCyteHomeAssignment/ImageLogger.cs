using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainUI
{
	public interface ILogger
	{
		void LogInformation(string message);
		void LogWarning(string message);
		void LogError(string message, Exception ex = null);
	}

	public class Logger : ILogger
	{
		private readonly string _logFilePath;

		public Logger(string logFilePath)
		{
			_logFilePath = logFilePath;
		}

		public void LogInformation(string message)
		{
			Log("INFO", message);
		}

		public void LogWarning(string message)
		{
			Log("WARNING", message);
		}

		public void LogError(string message, Exception ex = null)
		{
			Log("ERROR", message + (ex != null ? $" Exception: {ex}" : ""));
		}

		private void Log(string level, string message)
		{
			string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
			try
			{
				File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
			}
			catch (Exception ex)
			{
				// If we can't write to the log file, write to console as a fallback
				Console.WriteLine($"Failed to write to log file: {ex.Message}");
				Console.WriteLine(logMessage);
			}
		}
	}
}
