using FMODUnity;
using InfiniteCanvas.InkIntegration.Parsers.Audio;
using Sirenix.OdinInspector;

namespace InfiniteCanvas.InkIntegration.Editor
{
	public class AudioLibraryBuilder
	{
		[InlineEditor(Expanded = true), AssetList(AutoPopulate = true)]
		public AudioLibrary AudioLibrary;

		[Button]
		public void FetchEvents()
		{
			AudioLibrary.LibraryItems.Clear();
			foreach (var eventRef in EventManager.Events)
			{
				if (eventRef.Path.StartsWith("snapshot")) { continue; }

				var eventReference = new EventReference() { Guid = eventRef.Guid, Path = eventRef.Path };
				AudioLibrary.LibraryItems.Add(new AudioLibrary.LibraryItem(eventRef.name.Split('/')[^1].Replace(' ', '_'),
				                                                           eventReference));
			}
		}
	}
}