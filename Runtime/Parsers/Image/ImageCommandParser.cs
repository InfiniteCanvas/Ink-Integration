using System;
using InfiniteCanvas.Utilities.Extensions;
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

		public void Initialize() => _log.Information("Initializing image parser");

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
	}
}