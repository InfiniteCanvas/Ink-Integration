using System;
using System.Collections.Generic;
using FMODUnity;
using InfiniteCanvas.Utilities.Extensions;
using UnityEngine;
using VContainer.Unity;
using ILogger = Serilog.ILogger;

namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	public class AudioCommandParser : IAudioCommandParser, IInitializable
	{
		private const    string       _PARAMS_NAME_LABEL               = "pnl:";
		private const    string       _PARAMS_NAME                     = "pn:";
		private const    string       _POSITION                        = "p:";
		private const    string       _AUDIO_ACTION                    = "a:";
		private const    string       _AUDIO_STOP                      = "a:stop";
		private const    string       _AUDIO_PLAY                      = "a:play";
		private const    string       _AUDIO_TOGGLE                    = "a:toggle";
		private const    string       _AUDIO_REMOVE                    = "a:remove";
		private const    string       _MULTI_VALUE_PARAMETER_DELIMITER = ";";
		private const    string       _PARAMETER_DELIMITER             = ">";
		private readonly AudioLibrary _audioLibrary;
		private readonly ILogger      _log;

		public AudioCommandParser(ILogger logger, AudioLibrary audioLibrary)
		{
			_audioLibrary = audioLibrary;
			_log = logger.ForContext<AudioCommandParser>();
		}

		public bool ParseCommand(in  string            command,
		                         out EventReference    eventReference,
		                         out bool              isOneShot,
		                         out Vector3           position,
		                         out AudioAction       audioAction,
		                         List<AudioParameters> parameters)
		{
			// set defaults
			eventReference = default;
			isOneShot = true;
			position = Vector3.zero;
			audioAction = AudioAction.Play;
			parameters ??= new List<AudioParameters>();

			var span = command.AsSpan();
			var delimiterIndices = span.IndicesOf(_PARAMETER_DELIMITER);

			if (delimiterIndices.Length == 0)
			{
				isOneShot = true;
				eventReference = _audioLibrary.GetValueOrDefault(command.GetCustomHashCode());
				if (!eventReference.IsNull) return true;
				_log.Error("EventReference is null: {CommandText}", command);
				return false;
			}

			var eventReferenceSpan = span[..delimiterIndices[0]];
			eventReference = _audioLibrary.GetValueOrDefault(eventReferenceSpan.GetCustomHashCode());
			if (eventReference.IsNull)
			{
				// special case where we stop everything
				if (TryGetAudioAction(span[delimiterIndices[0]..], ref audioAction))
					if (audioAction == AudioAction.Stop)
						return true;

				_log.Error("EventReference is null: Raw : {CommandText}, Name: {EventName}", command, eventReferenceSpan.ToString());
				return false;
			}


			for (var i = 1; i <= delimiterIndices.Length; i++)
			{
				ReadOnlySpan<char> parameterSpan;

				if (i == delimiterIndices.Length)
				{
					parameterSpan = span[(delimiterIndices[^1] + 1)..];
				}
				else
				{
					var start = delimiterIndices[i - 1] + 1;
					var length = delimiterIndices[i]    - start;
					parameterSpan = span.Slice(start, length);

					_log.Debug("Raw slice: {Start}-{End} => {Value}",
					           start,
					           start + length,
					           parameterSpan.ToString());
				}

				_log.Debug("Parsing audio command parameter: {AudioCommandParameter}", parameterSpan.ToString());

				if (TryGetAudioAction(parameterSpan, ref audioAction))
				{
					isOneShot = false;
					continue;
				}

				if (TryGetPosition(parameterSpan, ref position))
					continue;
				if (TryGetNameParameter(parameterSpan, parameters))
					continue;
				if (TryGetNameLabelParameter(parameterSpan, parameters))
					continue;
			}

			return true;
		}

		public void Initialize() => _log.Information("Initializing Audio Command Parser");

		private bool TryGetNameLabelParameter(ReadOnlySpan<char> paramSpan, List<AudioParameters> parameters)
		{
			if (paramSpan.IndexOf(_PARAMS_NAME_LABEL) < 0)
				return false;

			_log.Verbose("Parsing name+value parameter");
			// go to name
			var paramSlice = paramSpan[_PARAMS_NAME_LABEL.Length..];

			// parse components (format: pnl:name:label)
			var nameIndex = paramSlice.IndexOf(_MULTI_VALUE_PARAMETER_DELIMITER);
			if (nameIndex < 0 || paramSlice.Length <= nameIndex + _MULTI_VALUE_PARAMETER_DELIMITER.Length)
			{
				_log.Error("Audio Parameter malformed: [{ParameterRaw}]", paramSlice.ToString());
				return false;
			}

			var labelSlice = paramSlice.Slice(nameIndex + _MULTI_VALUE_PARAMETER_DELIMITER.Length);

			var parsedParameters = new AudioParameters { Name = paramSlice[..nameIndex].ToString(), Label = labelSlice.ToString(), HasLabel = true };
			parameters.Add(parsedParameters);
			_log.Debug("Added audio parameters: {Parameter}", parsedParameters.ToString());
			return true;
		}

		// returns false if malformed
		private bool TryGetNameParameter(ReadOnlySpan<char> paramSpan, List<AudioParameters> parameters)
		{
			if (paramSpan.IndexOf(_PARAMS_NAME) < 0)
				return false;

			_log.Verbose("Parsing name parameter from {ParameterRaw}", paramSpan.ToString());
			// go to name
			var paramSlice = paramSpan[_PARAMS_NAME.Length..];

			// parse components (format: pn:name:value)
			var nameIndex = paramSlice.IndexOf(_MULTI_VALUE_PARAMETER_DELIMITER);
			if (nameIndex < 0 || paramSlice.Length <= nameIndex + _MULTI_VALUE_PARAMETER_DELIMITER.Length)
			{
				_log.Error("Audio Parameter malformed: [{ParameterRaw}]", paramSlice.ToString());
				return false;
			}

			var valueSlice = paramSlice.Slice(nameIndex + _MULTI_VALUE_PARAMETER_DELIMITER.Length);

			var name = paramSlice[..nameIndex];
			var valueParsed = float.TryParse(valueSlice, out var value);

			if (!valueParsed)
			{
				_log.Error("Audio Parameter malformed, cannot parse float: [{ParameterPartRaw}] from {ParameterRaw}", valueSlice.ToString(), paramSlice.ToString());
				return false;
			}

			var parsedParameters = new AudioParameters { Name = name.ToString(), Value = value };
			parameters.Add(parsedParameters);
			_log.Debug("Added audio parameters: {Parameter}", parsedParameters.ToString());
			return true;
		}

		private bool TryGetPosition(ReadOnlySpan<char> paramSpan, ref Vector3 position)
		{
			if (paramSpan.IndexOf(_POSITION, StringComparison.OrdinalIgnoreCase) < 0)
				return false;

			// go to vector data
			var vectorSpan = paramSpan[_POSITION.Length..];
			_log.Verbose("Parsing audio positions from {ParameterRaw}, with these positions: {VectorSpan}", paramSpan.ToString(), vectorSpan.ToString());

			// parse components (format: p:x:y:z)
			var xEndIndex = vectorSpan.IndexOf(_MULTI_VALUE_PARAMETER_DELIMITER);
			if (xEndIndex < 0)
			{
				_log.Error("Audio Position malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			var yStartSpan = vectorSpan.Slice(xEndIndex + _MULTI_VALUE_PARAMETER_DELIMITER.Length);
			var yEndIndex = yStartSpan.IndexOf(_MULTI_VALUE_PARAMETER_DELIMITER);
			if (yEndIndex < 0)
			{
				_log.Error("Audio Position malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			var zStartSpan = yStartSpan.Slice(yEndIndex + _MULTI_VALUE_PARAMETER_DELIMITER.Length);

			var xParsed = float.TryParse(vectorSpan.Slice(0, xEndIndex), out var x);
			var yParsed = float.TryParse(yStartSpan.Slice(0, yEndIndex), out var y);
			var zParsed = float.TryParse(zStartSpan,                     out var z);

			if (!xParsed || !yParsed || !zParsed)
			{
				_log.Error("Audio Position malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			position = new Vector3(x, y, z);

			return true;
		}

		private bool TryGetAudioAction(in ReadOnlySpan<char> paramSpan, ref AudioAction audioAction)
		{
			if (paramSpan.IndexOf(_AUDIO_ACTION, StringComparison.OrdinalIgnoreCase) < 0)
				return false;

			_log.Verbose("Parsing audio action");
			if (paramSpan.IndexOf(_AUDIO_PLAY, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				audioAction = AudioAction.Play;
				return true;
			}

			if (paramSpan.IndexOf(_AUDIO_STOP, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				audioAction = AudioAction.Stop;
				return true;
			}

			if (paramSpan.IndexOf(_AUDIO_TOGGLE, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				audioAction = AudioAction.TogglePause;
				return true;
			}

			if (paramSpan.IndexOf(_AUDIO_REMOVE, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				audioAction = AudioAction.Remove;
				return true;
			}

			return false;
		}
	}
}