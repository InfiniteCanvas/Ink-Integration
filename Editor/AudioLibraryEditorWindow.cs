using FMODUnity;
using InfiniteCanvas.InkIntegration.Parsers.Audio;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using EventReference = FMODUnity.EventReference;

namespace InfiniteCanvas.InkIntegration.Editor
{
	public class AudioLibraryEditorWindow : OdinEditorWindow
	{
		[AssetList(AutoPopulate = true), Required]
		public AudioLibrary AudioLibrary;

		[MenuItem("Tools/Infinite Canvas/Audio Library Tool")]
		private static void OpenWindow() { GetWindow<AudioLibraryEditorWindow>().Show(); }

		[Button]
		public void FetchEvents()
		{
			AudioLibrary.LibraryItems.Clear();
			foreach (var eventRef in EventManager.Events)
			{
				// ignore snapshots
				if (eventRef.Path.StartsWith("snapshot")) { continue; }

				var eventReference = new EventReference() { Guid = eventRef.Guid, Path = eventRef.Path };
				AudioLibrary.LibraryItems.Add(new AudioLibrary.LibraryItem(eventRef.name.Split('/')[^1],
				                                                           eventReference));
			}
		}

		[Button]
		public void WriteEvents() { AudioLibrary.ClearAndWriteDictionary(); }
	}
}