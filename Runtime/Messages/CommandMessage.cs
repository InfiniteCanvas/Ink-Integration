namespace InfiniteCanvas.InkIntegration.Messages
{
	public readonly struct CommandMessage
	{
		public readonly CommandType CommandType;
		public readonly string      Text;

		public CommandMessage(CommandType commandType, string text)
		{
			CommandType = commandType;
			Text = text;
		}
	}

	public enum CommandType
	{
		None,
		Audio,
		Animation,
		Scene,
		UI,
		Image,
		Text,
		Other,
	}
}