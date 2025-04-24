using System.Collections.Generic;
using Serilog;
using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;
using UnityEngine;
using VContainer.Unity;
using ILogger = Serilog.ILogger;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	public partial class ImageCommandParser : IImageCommandParser, IInitializable
	{
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
			Log.Verbose("{ImageNamespace}:{ImagePose}", nsAndPose.Namespace, nsAndPose.Pose);

			if (parameters == null) return cmd;

			foreach (var param in parameters)
			{
				Log.Verbose("{ImageParamKind}: {ImageParamValue}", param.Kind, param.Value);
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
	}
}