using System;
using System.Collections.Generic;
using FMODUnity;
using InfiniteCanvas.Utilities.Extensions;
using Serilog;
using Sirenix.OdinInspector;
using UnityEngine;

namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	[CreateAssetMenu(fileName = "Audio Library", menuName = "Infinite Canvas/Audio Library", order = 0)]
	public class AudioLibrary : SerializedScriptableObject
	{
		[ReadOnly] public Dictionary<int, EventReference> Events = new();

		[TableList] public List<LibraryItem> LibraryItems = new();

		public EventReference GetValueOrDefault(int eventHash) => Events.GetValueOrDefault(eventHash);

		[ButtonGroup("Database")]
		public void ClearAndWriteDictionary()
		{
			if (Events == null)
				Events = new Dictionary<int, EventReference>();
			Events.Clear();
			foreach (var item in LibraryItems)
			{
				if (!Events.TryAdd(item.EventName.GetCustomHashCode(), item.EventReference))
					Log.Warning("Item could not be added to EventReferenceDatabase: {EventName} : {EventReference}", item.EventName, item.EventReference);
			}
		}


		[Serializable]
		public class LibraryItem
		{
			public string         EventName;
			public EventReference EventReference;

			public LibraryItem(string eventName, EventReference eventReference)
			{
				EventName = eventName;
				EventReference = eventReference;
			}
		}
	}
}