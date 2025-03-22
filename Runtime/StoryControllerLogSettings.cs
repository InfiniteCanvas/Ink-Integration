using System;

namespace InfiniteCanvas.InkIntegration
{
	public class StoryControllerLogSettings
	{
		public enum LogLevel
		{
			Verbose,
			Debug,
			Info,
			Warning,
			Error,
			Fatal,
		}

		public LogLevel                 Level;
		public Action<LogLevel, string> LogAction;

		public StoryControllerLogSettings(LogLevel level, Action<LogLevel, string> logAction)
		{
			Level = level;
			LogAction = logAction;
		}
	}

	public static class LogLevelExtensions
	{
		public static void LogIf(this StoryControllerLogSettings settings, StoryControllerLogSettings.LogLevel levelToLog, string message)
		{
			if (settings.Level <= levelToLog)
				settings.LogAction?.Invoke(levelToLog, message);
		}
	}
}