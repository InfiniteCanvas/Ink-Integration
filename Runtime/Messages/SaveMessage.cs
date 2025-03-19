namespace InfiniteCanvas.InkIntegration.Messages
{
	public struct SaveMessage
	{
		public string SaveFilePath;

		public SaveMessage(string saveFilePath) => SaveFilePath = saveFilePath;

		public static implicit operator string(SaveMessage message) => message.SaveFilePath;

		public static implicit operator SaveMessage(string saveFilePath) => new(saveFilePath);
	}
}