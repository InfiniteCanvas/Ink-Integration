namespace InfiniteCanvas.InkIntegration.Messages
{
	public readonly struct ContinueMessage
	{
		/// <summary>
		///     Continues until next command or choice. Don't use if you don't want to miss tags!
		/// </summary>
		public readonly bool Maximally;

		public ContinueMessage(bool maximally) => Maximally = maximally;

		public static implicit operator ContinueMessage(bool maximally) => new(maximally);

		public static implicit operator bool(ContinueMessage message) => message.Maximally;
	}
}