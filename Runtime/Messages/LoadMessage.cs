namespace InfiniteCanvas.InkIntegration.Messages
{
	public struct LoadMessage
	{
		public string LoadFilePath;

		public LoadMessage(string loadFilePath) => LoadFilePath = loadFilePath;

		public static implicit operator LoadMessage(string loadFilePath) => new(loadFilePath);

		public static implicit operator string(LoadMessage message) => message.LoadFilePath;
	}
}