namespace InfiniteCanvas.InkIntegration.Messages
{
	public readonly struct ChoiceSelectedMessage
	{
		public readonly int Index;

		public ChoiceSelectedMessage(int index) => Index = index;

		public static implicit operator ChoiceSelectedMessage(int index) => new(index);

		public static implicit operator int(ChoiceSelectedMessage message) => message.Index;
	}
}