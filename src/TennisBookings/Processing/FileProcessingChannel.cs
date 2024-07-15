using System.Threading.Channels;

namespace TennisBookings.Processing;

public class FileProcessingChannel
{
	private const int MaxMessagesInChannel = 100;

	private readonly Channel<string> _channel;
	private readonly ILogger<FileProcessingChannel> _logger;

	public FileProcessingChannel(ILogger<FileProcessingChannel> logger)
	{
		var options = new BoundedChannelOptions(MaxMessagesInChannel)
		{
			SingleWriter = false,
			SingleReader = true
		};

		_channel = Channel.CreateBounded<string>(options);

		_logger = logger;
	}

	public async Task<bool> AddFileAsync(string fileName, CancellationToken ct = default)
	{
		while (await _channel.Writer.WaitToWriteAsync(ct) && !ct.IsCancellationRequested)
		{
			if (_channel.Writer.TryWrite(fileName))
			{
				Log.ChannelMessageWritten(_logger, fileName);

				return true;
			}
		}

		return false;
	}

	public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct = default) =>
		_channel.Reader.ReadAllAsync(ct);

	public bool TryCompleteWriter(Exception? ex = null) => _channel.Writer.TryComplete(ex);

	internal static class EventIds
	{
		public static readonly EventId ChannelMessageWritten = new(100, "ChannelMessageWritten");
	}

	private static class Log
	{
		private static readonly Action<ILogger, string, Exception?> _channelMessageWritten = LoggerMessage.Define<string>(
			LogLevel.Information,
			EventIds.ChannelMessageWritten,
			"Filename {FileName} was written to the channel.");

		public static void ChannelMessageWritten(ILogger logger, string fileName)
		{
			_channelMessageWritten(logger, fileName, null);
		}
	}
}
