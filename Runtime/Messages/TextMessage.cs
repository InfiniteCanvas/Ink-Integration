namespace InfiniteCanvas.InkIntegration.Messages
{
	public struct TextMessage
	{
		public readonly string Text;

		public TextMessage(string text) => Text = text;

		public static implicit operator TextMessage(string text) => new(text);

		public static implicit operator string(TextMessage text) => text.Text;
	}
}