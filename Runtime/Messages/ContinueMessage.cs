namespace InfiniteCanvas.InkIntegration.Messages
{
	public struct ContinueMessage
	{
		public bool Maximally;

		public ContinueMessage(bool maximally) => Maximally = maximally;

		public static implicit operator ContinueMessage(bool maximally) => new(maximally);

		public static implicit operator bool(ContinueMessage message) => message.Maximally;
	}
}