using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using InfiniteCanvas.InkIntegration.Messages;
using InfiniteCanvas.Utilities.Extensions;
using MessagePipe;
using Superpower;
using VContainer.Unity;
using ILogger = Serilog.ILogger;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	public class AudioCommandProcessor : IInitializable, IDisposable
	{
		private readonly Dictionary<GUID, EventInstance> _activeEvents = new();
		private readonly AudioLibrary                    _audioLibrary;
		private readonly IDisposable                     _disposable;
		private readonly ILogger                         _log;
		private readonly IAudioCommandParser             _parser;

		public AudioCommandProcessor(ILogger                          logger,
		                             ISubscriber<CommandMessage>      commandSubscriber,
		                             IAsyncSubscriber<CommandMessage> commandAsyncSubscriber,
		                             AudioLibrary                     audioLibrary,
		                             IAudioCommandParser              parser)
		{
			_audioLibrary = audioLibrary;
			_parser = parser;
			_log = logger.ForContext<AudioCommandProcessor>();
			var bag = DisposableBag.CreateBuilder();
			commandSubscriber.Subscribe(AudioCommandHandler).AddTo(bag);
			commandAsyncSubscriber.Subscribe((message, _) =>
			                                 {
				                                 AudioCommandHandler(message);
				                                 return UniTask.CompletedTask;
			                                 })
			                      .AddTo(bag);
			_disposable = bag.Build();
		}

		public void Dispose() => _disposable?.Dispose();

		public void Initialize() => _log.Debug("Initializing AudioPlayer");


		private void PlayAudio(AudioCommand audioCommand)
		{
			var eventReference = _audioLibrary.GetValueOrDefault(audioCommand.EventName.GetCustomHashCode());
			if (eventReference.IsNull)
			{
				// special case where we stop all audio
				if (audioCommand.AudioAction == AudioAction.Stop)
					StopAllAudio();
				else
					_log.Error("EventReference is null");

				return;
			}

			_log.Information("Playing event: {EventReference}, at {Position}, {AudioAction} as {AudioKind} with {Parameters}",
			                 eventReference,
			                 audioCommand.Position,
			                 audioCommand.AudioAction,
			                 audioCommand.IsOneShot ? "OneShot" : "Tracked Instance",
			                 audioCommand.Parameters);

			if (audioCommand.IsOneShot)
			{
				RuntimeManager.PlayOneShot(eventReference, audioCommand.Position);
				return;
			}

			if (!_activeEvents.TryGetValue(eventReference.Guid, out var instance))
			{
				if (audioCommand.AudioAction != AudioAction.Play)
				{
					_log.Warning("Trying to {AudioAction} on a non-tracked instance for {EventReference}", audioCommand.AudioAction, eventReference);
					return;
				}

				var eventInstance = RuntimeManager.CreateInstance(eventReference);
				eventInstance.set3DAttributes(audioCommand.Position.To3DAttributes());
				ApplyParameters(ref eventInstance);
				eventInstance.start();
				_activeEvents.Add(eventReference.Guid, eventInstance);
				return;
			}

			instance.set3DAttributes(audioCommand.Position.To3DAttributes());
			switch (audioCommand.AudioAction)
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
				case AudioAction.Reset:
					instance.setTimelinePosition(0);
					break;
				case AudioAction.Remove:
					instance.stop(STOP_MODE.IMMEDIATE);
					instance.release();
					_activeEvents.Remove(eventReference.Guid);
					break;
				case AudioAction.None:
				default: break;
			}

			return;

			void ApplyParameters(ref EventInstance eventInstance)
			{
				if (audioCommand.Parameters.IsNullOrEmpty()) return;
				foreach (var parameter in audioCommand.Parameters!)
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

			try
			{
				var audioCommand = _parser.ParseCommand(message.Text);
				_log.Verbose("{AudioCommand}", audioCommand);
				PlayAudio(audioCommand);
			}
			catch (ParseException e)
			{
				_log.Error(e, "Error parsing command: {AudioCommand}", message.Text);
			}
		}
	}
}