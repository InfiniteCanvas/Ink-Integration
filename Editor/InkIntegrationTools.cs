using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace InfiniteCanvas.InkIntegration.Editor
{
	public class InkIntegrationTools : OdinMenuEditorWindow
	{
		[MenuItem("Tools/Infinite Canvas/Ink Integration Tools")]
		private static void OpenWindow() { GetWindow<InkIntegrationTools>().Show(); }

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree();

			tree.Add("Audio",  new AudioLibraryBuilder());
			tree.Add("Images", new ImageLibraryBuilder());

			return tree;
		}
	}
}