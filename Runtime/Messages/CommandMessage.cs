namespace InfiniteCanvas.InkIntegration.Messages
{
	public struct CommandMessage
	{
		public LineType LineType;
		public string   Text;

		public CommandMessage(LineType lineType, string text)
		{
			LineType = lineType;
			Text = text;
		}
	}

	public enum LineType
	{
		Audio,
		Animation,
		Scene,
		UI,
		Other,
		Text,
	}
}