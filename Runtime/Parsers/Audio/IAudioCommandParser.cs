using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	public interface IAudioCommandParser
	{
		[Obsolete("Use `AudioCommand ParseCommand(in string command)` instead.")]
		public bool ParseCommand(in  string            command,
		                         out EventReference    eventReference,
		                         out bool              isOneShot,
		                         out Vector3           position,
		                         out AudioAction       audioAction,
		                         List<AudioParameters> parameters);

		public AudioCommand ParseCommand(in string command);
	}

	public struct AudioCommand
	{
		public string            EventName;
		public AudioAction       AudioAction;
		public bool              IsOneShot;
		public Vector3           Position;
		public AudioParameters[] Parameters;

		public override string ToString()
			=> $"{nameof(EventName)}: {EventName}, {nameof(AudioAction)}: {AudioAction}, {nameof(IsOneShot)}: {IsOneShot}, {nameof(Position)}: {Position}, {nameof(Parameters)}: {string.Join(", ", Parameters)}";
	}
}