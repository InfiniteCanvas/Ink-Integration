using System;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using InfiniteCanvas.InkIntegration.Messages;
using InfiniteCanvas.Utilities.Extensions;
using MessagePipe;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;
using ILogger = Serilog.ILogger;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	public class AudioPlayer : IInitializable, IDisposable
	{
		private readonly Dictionary<GUID, EventInstance> _activeEvents = new();
		private readonly IDisposable                     _disposable;
		private readonly ILogger                         _log;
		private readonly IAudioCommandParser             _parser;

		public AudioPlayer(ILogger logger, ISubscriber<CommandMessage> commandSubscriber, IAudioCommandParser parser)
		{
			_parser = parser;
			_log = logger.ForContext<AudioPlayer>();
			_disposable = commandSubscriber.Subscribe(AudioCommandHandler);
		}

		public void Dispose() => _disposable?.Dispose();

		public void Initialize() => _log.Debug("Initializing AudioPlayer");


		private void PlayAudio(EventReference        eventReference,
		                       bool                  isOneShot   = true,
		                       Vector3               position    = default,
		                       AudioAction           audioAction = AudioAction.Play,
		                       List<AudioParameters> parameters  = null)
		{
			if (eventReference.IsNull)
			{
				// special case where we stop all audio
				if (audioAction == AudioAction.Stop)
					StopAllAudio();
				else
					_log.Error("EventReference is null");

				return;
			}

			_log.Information("Playing event: {EventReference}, at {Position}, {AudioAction} as {AudioKind} with {Parameters}",
			                 eventReference,
			                 position,
			                 audioAction,
			                 isOneShot ? "OneShot" : "Tracked Instance",
			                 parameters);

			if (isOneShot)
			{
				RuntimeManager.PlayOneShot(eventReference, position);
				return;
			}

			if (!_activeEvents.TryGetValue(eventReference.Guid, out var instance))
			{
				if (audioAction != AudioAction.Play)
				{
					_log.Warning("Trying to {AudioAction} on a non-tracked instance for {EventReference}", audioAction, eventReference);
					return;
				}

				var eventInstance = RuntimeManager.CreateInstance(eventReference);
				eventInstance.set3DAttributes(position.To3DAttributes());
				ApplyParameters(ref eventInstance);
				eventInstance.start();
				_activeEvents.Add(eventReference.Guid, eventInstance);
				return;
			}

			instance.set3DAttributes(position.To3DAttributes());
			switch (audioAction)
			{
				case AudioAction.Play:
					instance.start();
					ApplyParameters(ref instance);
					break;
				case AudioAction.TogglePause:
					instance.getPaused(out var isPaused);
					instance.setPaused(!isPaused);
					ApplyParameters(ref instance);
					break;
				case AudioAction.Stop:
					instance.stop(STOP_MODE.IMMEDIATE);
					break;
				case AudioAction.Remove:
				default:
					instance.stop(STOP_MODE.IMMEDIATE);
					instance.release();
					_activeEvents.Remove(eventReference.Guid);
					break;
			}

			return;

			void ApplyParameters(ref EventInstance eventInstance)
			{
				if (parameters.IsNullOrEmpty()) return;
				foreach (var parameter in parameters!)
				{
					if (parameter.HasLabel)
						eventInstance.setParameterByNameWithLabel(parameter.Name, parameter.Label);
					else
						eventInstance.setParameterByName(parameter.Name, parameter.Value);
				}
			}
		}

		private void StopAllAudio()
		{
			_log.Debug("Stopping all audio");
			foreach (var (key, value) in _activeEvents)
			{
				value.stop(STOP_MODE.IMMEDIATE);
				value.release();
			}

			_activeEvents.Clear();
		}

		private void AudioCommandHandler(CommandMessage message)
		{
			if (message.CommandType != CommandType.Audio) return;

			var parameters = ListPool<AudioParameters>.Get();
			parameters.Clear();
			if (_parser.ParseCommand(message.Text,
			                         out var eventReference,
			                         out var isOneShot,
			                         out var position,
			                         out var audioAction,
			                         parameters)) PlayAudio(eventReference, isOneShot, position, audioAction, parameters);
			else _log.Error("Parsing command failed");

			parameters.Release();
		}
	}
}