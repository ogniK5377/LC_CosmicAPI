using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace LC_CosmicAPI
{
	internal sealed class FilteredDiskLogListener : DiskLogListener
	{
		private readonly string Source;
		public FilteredDiskLogListener(string filteredSource, string localPath, int flushTime, LogLevel displayedLogLevel = LogLevel.Info, bool appendLog = false, bool includeUnityLog = false) : base(localPath, displayedLogLevel, appendLog, includeUnityLog)
		{
			Source = filteredSource;
			base.FlushTimer.Change(flushTime, flushTime);
		}

		public new void LogEvent(object sender, LogEventArgs eventArgs) {
			if (eventArgs.Source.SourceName != Source) return;
			base.LogEvent(sender, eventArgs);
		}
	}
}
