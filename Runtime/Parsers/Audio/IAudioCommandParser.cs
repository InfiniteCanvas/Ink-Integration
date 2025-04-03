using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	public interface IAudioCommandParser
	{
		public bool ParseCommand(in  string            command,
		                         out EventReference    eventReference,
		                         out bool              isOneShot,
		                         out Vector3           position,
		                         out AudioAction       audioAction,
		                         List<AudioParameters> parameters);
	}
}