using System;
using System.Collections.Generic;
using System.Linq;
using InfiniteCanvas.Utilities.Extensions;
using Superpower;
using Superpower.Parsers;
using UnityEngine;
using VContainer.Unity;
using ILogger = Serilog.ILogger;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	public class ImageCommandParser : IImageCommandParser, IInitializable
	{
		private const    string  _DEFAULT_HASH = "default";
		private const    string  _POSITION     = "p:";
		private const    string  _SCALE        = "s:";
		private const    string  _SCREEN_SPACE = "l:s";
		private const    string  _WORLD_SPACE  = "l:w";
		private readonly ILogger _log;

		public ImageCommandParser(ILogger logger) => _log = logger.ForContext<ImageCommandParser>();

		public void Initialize() => _log.Information("Initializing image parser");

		public ImageCommand ParseCommand(in string command) => _commandParser.Parse(command);

	#region Parser Combinator

		private static readonly TextParser<char> _valueDelimiter =
			Character.EqualTo(';').Or(Character.EqualTo(':'));

		private static readonly TextParser<char> _parameterDelimiter =
			Character.EqualTo(' ').Or(Character.EqualTo('>'));

		// Parse an identifier (letters, digits, underscores)
		private static readonly TextParser<string> _identifier =
			from first in Character.Letter
			from rest in Character.LetterOrDigit.Or(Character.EqualTo('_')).Many() //allows alphanumeric with _
			select new string(new[] { first }.Concat(rest).ToArray());

		private static readonly TextParser<float> _float =
			from num in Numerics.Decimal
			select float.Parse(num.ToString());

		private static readonly TextParser<(string Namespace, string Pose)> _namespaceAndPose =
			from ns in _identifier
			from pose in (
				from _ in _valueDelimiter
				from p in _identifier
				select p).OptionalOrDefault("default")
			select (ns, pose);

		// Parse the position parameter (p:x;y;z)
		private static readonly TextParser<Vector3> _positionParameter =
			from _ in Span.EqualTo("p:")
			from x in _float
			from _1 in _valueDelimiter
			from y in _float
			from z in _valueDelimiter.IgnoreThen(_float).Optional()
			select new Vector3(x, y, z ?? 0f);

		// Parse the scale parameter (s:x;y;z)
		private static readonly TextParser<Vector3> _scaleParameter =
			from _ in Span.EqualTo("s:")
			from x in _float
			from _1 in _valueDelimiter
			from y in _float
			from z in _valueDelimiter.IgnoreThen(_float).Optional()
			select new Vector3(x, y, z ?? 1f);

		// Parse the location parameter (l:w or l:s)
		private static readonly TextParser<bool> _locationParameter =
			from _ in Span.EqualTo("l:")
			from loc in Character.EqualTo('s').Value(true)
			select loc;

		// Parse any parameter
		private static readonly TextParser<(string Type, object Value)> _parameter =
			from delim in _parameterDelimiter
			from param in _positionParameter.Select(p => ("position", (object)p))
			                                .Try()
			                                .Or(_scaleParameter.Select(s => ("scale", (object)s)).Try())
			                                .Or(_locationParameter.Select(l => ("location", (object)l)))
			select param;

		// The complete parser for an image command
		private static readonly TextParser<ImageCommand> _commandParser =
			// from prefix in Prefix
			from nsAndPose in _namespaceAndPose
			from parameters in _parameter.Many()
			select BuildImageCommand(nsAndPose, parameters);

		private static ImageCommand BuildImageCommand((string Namespace, string Pose)          nsAndPose,
		                                              IEnumerable<(string Type, object Value)> parameters)
		{
			var cmd = new ImageCommand();
			cmd.Namespace = nsAndPose.Namespace;
			cmd.Pose = nsAndPose.Pose;

			foreach (var param in parameters)
			{
				switch (param.Type)
				{
					case "position":
						cmd.Position = (Vector3)param.Value;
						cmd.ModifyPosition = true;
						break;
					case "scale":
						cmd.Scale = (Vector3)param.Value;
						cmd.ModifyScale = true;
						break;
					case "location":
						cmd.IsScreenSpace = (bool)param.Value;
						break;
				}
			}

			return cmd;
		}

	#endregion

	#region Obsolete

		public bool ParseCommand(in  string             command,
		                         out ReadOnlySpan<char> imageNameSpace,
		                         out ReadOnlySpan<char> imagePose,
		                         out Vector3            position,
		                         out Vector3            scale,
		                         out bool               isScreenSpace)
		{
			position = Vector3.zero;
			scale = Vector3.one;
			isScreenSpace = false;
			var commandSpan = command.AsSpan();

			var delimiterIndices = commandSpan.IndicesOf(ParserUtilities.PARAMETER_DELIMITER);

			// no parameters
			if (delimiterIndices.Length == 0)
			{
				var indexOf = commandSpan.IndexOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);
				if (indexOf < 0)
				{
					imageNameSpace = commandSpan[..indexOf];
					imagePose = commandSpan[(indexOf + 1)..];
					return true;
				}

				imageNameSpace = commandSpan;
				imagePose = _DEFAULT_HASH;
				return true;
			}

			var parameterSpan = commandSpan[..delimiterIndices[0]];
			{
				_log.Verbose("Name and Pose: {Start}-{End} => {Value}", 0, delimiterIndices[0], parameterSpan.ToString());
				var indexOf = parameterSpan.IndexOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);
				if (indexOf < 0)
				{
					imageNameSpace = parameterSpan;
					imagePose = _DEFAULT_HASH;
				}
				else
				{
					imageNameSpace = parameterSpan[..indexOf];
					imagePose = parameterSpan[(indexOf + 1)..];
				}
			}

			for (var i = 1; i <= delimiterIndices.Length; i++)
			{
				if (i == delimiterIndices.Length)
				{
					parameterSpan = commandSpan[(delimiterIndices[^1] + 1)..];
				}
				else
				{
					var start = delimiterIndices[i - 1] + 1;
					var length = delimiterIndices[i]    - start;
					parameterSpan = commandSpan.Slice(start, length);

					_log.Verbose("Raw slice: {Start}-{End} => {Value}",
					             start,
					             start + length,
					             parameterSpan.ToString());
				}

				if (TryGetPosition(parameterSpan, out position))
				{
					_log.Verbose("Set Position to {ImagePosition}, Space is {ImageSpace}", position, isScreenSpace ? "ScreenSpace" : "WorldSpace");
					continue;
				}

				if (TryGetScale(parameterSpan, out scale))
				{
					_log.Verbose("Set Scale to {ImageScale}", scale);
					continue;
				}

				if (TryGetSpace(parameterSpan, out isScreenSpace)) _log.Verbose("Set Space to {ImageSpace}", isScreenSpace ? "ScreenSpace" : "WorldSpace");
			}

			return true;
		}


		private bool TryGetSpace(ReadOnlySpan<char> paramSpan, out bool isScreenSpace)
		{
			isScreenSpace = false;
			if (paramSpan.IndexOf(_SCREEN_SPACE) >= 0)
			{
				isScreenSpace = true;
				return true;
			}

			if (paramSpan.IndexOf(_WORLD_SPACE) >= 0)
			{
				isScreenSpace = false;
				return true;
			}

			return false;
		}

		private bool TryGetPosition(ReadOnlySpan<char> paramSpan, out Vector3 position)
		{
			position = default;
			if (paramSpan.IndexOf(_POSITION, StringComparison.OrdinalIgnoreCase) < 0)
				return false;

			// go to vector data
			var vectorSpan = paramSpan[_POSITION.Length..];
			_log.Verbose("Parsing image positions from {ParameterRaw}, with these positions: {VectorSpan}", paramSpan.ToString(), vectorSpan.ToString());
			var indices = vectorSpan.IndicesOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);

			// parse components (format: p:x:y:z)
			var xEndIndex = vectorSpan.IndexOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);
			if (xEndIndex < 0)
			{
				_log.Error("Image Position malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			var yStartSpan = vectorSpan.Slice(xEndIndex + ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER.Length);
			var yEndIndex = yStartSpan.IndexOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);
			if (yEndIndex < 0)
			{
				_log.Error("Image Position malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			var zStartSpan = yStartSpan.Slice(yEndIndex + ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER.Length);

			var xParsed = float.TryParse(vectorSpan.Slice(0, xEndIndex), out var x);
			var yParsed = float.TryParse(yStartSpan.Slice(0, yEndIndex), out var y);
			var zParsed = float.TryParse(zStartSpan,                     out var z);

			if (indices.Length < 3)
			{
				position = new Vector3(x, y, 0);
				return true;
			}

			if (!xParsed || !yParsed || !zParsed)
			{
				_log.Error("Image Position malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			position = new Vector3(x, y, z);

			return true;
		}

		private bool TryGetScale(ReadOnlySpan<char> paramSpan, out Vector3 scale)
		{
			scale = Vector3.one;
			if (paramSpan.IndexOf(_SCALE, StringComparison.OrdinalIgnoreCase) < 0)
				return false;

			// go to vector data
			var vectorSpan = paramSpan[_SCALE.Length..];
			_log.Verbose("Parsing image scales from {ParameterRaw}, with these scales: {VectorSpan}", paramSpan.ToString(), vectorSpan.ToString());
			var indices = vectorSpan.IndicesOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);

			// parse components (format: p:x:y:z)
			var xEndIndex = vectorSpan.IndexOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);
			if (xEndIndex < 0)
			{
				_log.Error("Image Scales malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			var yStartSpan = vectorSpan.Slice(xEndIndex + ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER.Length);
			var yEndIndex = yStartSpan.IndexOf(ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER);
			if (yEndIndex < 0)
			{
				_log.Error("Image Scales malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			var zStartSpan = yStartSpan.Slice(yEndIndex + ParserUtilities.MULTI_VALUE_PARAMETER_DELIMITER.Length);

			var xParsed = float.TryParse(vectorSpan.Slice(0, xEndIndex), out var x);
			var yParsed = float.TryParse(yStartSpan.Slice(0, yEndIndex), out var y);
			var zParsed = float.TryParse(zStartSpan,                     out var z);

			if (indices.Length < 3)
			{
				scale = new Vector3(x, y, 1);
				return true;
			}

			if (!xParsed || !yParsed || !zParsed)
			{
				_log.Error("Image Scales malformed: [{VectorSpan}]", vectorSpan.ToString());
				return false;
			}

			scale = new Vector3(x, y, z);

			return true;
		}

	#endregion
	}
}