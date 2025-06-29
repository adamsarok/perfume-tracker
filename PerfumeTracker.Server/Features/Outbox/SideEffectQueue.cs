namespace PerfumeTracker.Server.Features.Outbox;

using System.Threading.Channels;
public interface ISideEffectQueue {
	void Enqueue(OutboxMessage message);
	ChannelReader<OutboxMessage> Reader { get; }
}

public class SideEffectQueue : ISideEffectQueue {
	private readonly Channel<OutboxMessage> _channel;

	public SideEffectQueue() {
		_channel = Channel.CreateUnbounded<OutboxMessage>();
	}

	public void Enqueue(OutboxMessage message) {
		if (!_channel.Writer.TryWrite(message)) {
			throw new InvalidOperationException("Failed to enqueue side effect.");
		}
	}

	public ChannelReader<OutboxMessage> Reader => _channel.Reader;
}