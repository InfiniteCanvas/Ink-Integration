namespace InfiniteCanvas.InkIntegration.Messages
{
	public struct TextMessage
	{
		public readonly string Text;
		public readonly bool   HasTags;

		public TextMessage(string text, bool hasTags = false)
		{
			Text = text;
			HasTags = hasTags;
		}

		public static implicit operator TextMessage(string text) => new(text);

		public static implicit operator string(TextMessage text) => text.Text;

		public static TextMessage WithTag(string text) => new(text, true);

		public void Deconstruct(out string text, out bool hasTags)
		{
			text = Text;
			hasTags = HasTags;
		}
	}
}