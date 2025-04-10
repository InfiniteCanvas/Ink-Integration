using InfiniteCanvas.InkIntegration.Parsers.Image;
using Sirenix.OdinInspector;

namespace InfiniteCanvas.InkIntegration.Editor
{
	public class ImageLibraryBuilder
	{
		[InlineEditor(Expanded = true), AssetList(AutoPopulate = true)]
		public ImageLibrary ImageLibrary;

		public string[] SearchInFolders = { "Assets/Sprites" };

		[Button]
		public void GenerateImageLibrary()
		{
			ImageLibrary.GetImagePaths(SearchInFolders);
			ImageLibrary.WriteImagePathsToLibrary();
		}
	}
}