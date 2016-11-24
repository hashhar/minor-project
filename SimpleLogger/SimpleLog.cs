using System;
using System.IO;

namespace SimpleLogger
{
	static class SimpleLog
	{
		private static DirectoryInfo _logDir = new DirectoryInfo(Directory.GetCurrentDirectory());
		private static string _fileName;

		public static string LogDir { get; } = _logDir.FullName;
		public static string FileName => _fileName;

		//public SimpleLog()
		//{
		//	AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
		//}

		public static void SetLogInfo(string logDir = null, string fileName = "application.log", bool truncate = true)
		{
			SetLogDir(logDir);
			SetLogFile(fileName, truncate);
		}

		private static void SetLogFile(string fileName, bool truncate)
		{
			_fileName = fileName;
			if (!truncate && File.Exists(_fileName))
				throw new AccessViolationException($"File '{_fileName}' already exists and will not be truncated!");
		}

		private static void SetLogDir(string logDir)
		{
			if (string.IsNullOrEmpty(logDir))
				logDir = Directory.GetCurrentDirectory();
			_logDir = new DirectoryInfo(logDir);

			if (!_logDir.Exists)
				_logDir.Create();
			else
				throw new DirectoryNotFoundException($"Directory '{_logDir.FullName}' does not exist!");
		}
	}
}
