using System;
using System.Collections.Generic;
using InfiniteCanvas.Utilities.Extensions;
using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;
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

		public ImageCommand ParseCommand(in string command) => _commandParser.Parse(_tokenizer.Tokenize(command));

		public void Initialize() => _log.Information("Initializing image parser");

	#region Parser Combinator

		private static readonly TextParser<string> _identifierTokens =
			from first in Character.Letter
			from rest in Character.LetterOrDigit.Or(Character.EqualTo('_')).Many() //allows alphanumeric with _
			select new string($"{first}{rest}");

		private static readonly Tokenizer<ImageTokenKind> _tokenizer = new TokenizerBuilder<ImageTokenKind>()
		                                                              .Match(Character.EqualTo(' '), ImageTokenKind.ParameterDelimiter)
		                                                              .Match(Character.EqualTo(':'), ImageTokenKind.ValueDelimiter)
		                                                              .Match(Character.EqualTo(','), ImageTokenKind.Comma)
		                                                              .Match(Numerics.Decimal,       ImageTokenKind.Number)
		                                                              .Match(Span.EqualTo("p:"),     ImageTokenKind.ParamPosition)
		                                                              .Match(Span.EqualTo("s:"),     ImageTokenKind.ParamScale)
		                                                              .Match(Span.EqualTo("ui"),     ImageTokenKind.ParamScreenSpace)
		                                                              .Match(_identifierTokens,      ImageTokenKind.Identifier)
		                                                              .Build();

		private enum CommandKind
		{
			None,
			Position,
			Scale,
			ScreenSpace,
		}

		private static readonly TokenListParser<ImageTokenKind, string> _identifier = Token.EqualTo(ImageTokenKind.Identifier).Select(t => t.ToStringValue());
		private static readonly TokenListParser<ImageTokenKind, float>  _float      = Token.EqualTo(ImageTokenKind.Number).Select(t => float.Parse(t.ToStringValue()));

		private static readonly TokenListParser<ImageTokenKind, (string Namespace, string Pose)> _namespacePose =
			from ns in _identifier
			from p in Token.EqualTo(ImageTokenKind.ValueDelimiter).IgnoreThen(_identifier).OptionalOrDefault("default")
			select (ns, p);

		private static readonly TokenListParser<ImageTokenKind, Vector3> _positionParameter =
			from _ in Token.EqualTo(ImageTokenKind.ParamPosition)
			from x in _float
			from y in Token.EqualTo(ImageTokenKind.Comma).IgnoreThen(_float)
			from z in Token.EqualTo(ImageTokenKind.Comma).IgnoreThen(_float).Optional()
			select new Vector3(x, y, z.GetValueOrDefault(0));

		private static readonly TokenListParser<ImageTokenKind, Vector3> _scaleParameter =
			from _ in Token.EqualTo(ImageTokenKind.ParamScale)
			from x in _float
			from y in Token.EqualTo(ImageTokenKind.Comma).IgnoreThen(_float)
			from z in Token.EqualTo(ImageTokenKind.Comma).IgnoreThen(_float).Optional()
			select new Vector3(x, y, z.GetValueOrDefault(1));

		private static readonly TokenListParser<ImageTokenKind, Vector3> _screenSpaceParameter =
			from screenSpace in Token.EqualTo(ImageTokenKind.ParamScreenSpace)
			select screenSpace.HasValue ? Vector3.one : Vector3.zero;

		private static readonly TokenListParser<ImageTokenKind, (CommandKind kind, Vector3 value)> _parameter =
			from _ in Token.EqualTo(ImageTokenKind.ParameterDelimiter)
			from p in _positionParameter.Select(v => (CommandKind.Position, p: v))
			                            .Try()
			                            .Or(_scaleParameter.Select(v => (CommandKind.Scale, p: v)).Try())
			                            .Or(_screenSpaceParameter.Select(v => (CommandKind.ScreenSpace, p: v)).Try())
			select p;

		private static readonly TokenListParser<ImageTokenKind, ImageCommand> _commandParser =
			from nsp in _namespacePose
			from parameters in _parameter.Many()
			select BuildImageCommand(nsp, parameters);


		private static ImageCommand BuildImageCommand((string Namespace, string Pose)                nsAndPose,
		                                              IEnumerable<(CommandKind Kind, Vector3 Value)> parameters)
		{
			var cmd = new ImageCommand { Namespace = nsAndPose.Namespace, Pose = nsAndPose.Pose };
			Debug.Log($"{nsAndPose.Namespace}:{nsAndPose.Pose}");

			if (parameters == null) return cmd;

			foreach (var param in parameters)
			{
				Debug.Log($"{param.Kind}: {param.Value}");
				switch (param.Kind)
				{
					case CommandKind.Position:
						cmd.Position = param.Value;
						cmd.ModifyPosition = true;
						break;
					case CommandKind.Scale:
						cmd.Scale = param.Value;
						cmd.ModifyScale = true;
						break;
					case CommandKind.ScreenSpace:
						cmd.IsScreenSpace = param.Value.x > 0;
						break;
					case CommandKind.None:
					default: break;
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
				if (i == delimiterIndices.Length) parameterSpan = commandSpan[(delimiterIndices[^1] + 1)..];
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