using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;
using UnityEngine;
using VContainer.Unity;
using ILogger = Serilog.ILogger;

namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	public partial class AudioCommandParser : IAudioCommandParser, IInitializable
	{
		private const    string       _PARAMS_NAME_LABEL = "pnl:";
		private const    string       _PARAMS_NAME       = "pn:";
		private const    string       _POSITION          = "p:";
		private const    string       _AUDIO_ACTION      = "a:";
		private const    string       _AUDIO_STOP        = "a:stop";
		private const    string       _AUDIO_PLAY        = "a:play";
		private const    string       _AUDIO_TOGGLE      = "a:toggle";
		private const    string       _AUDIO_REMOVE      = "a:remove";
		private readonly AudioLibrary _audioLibrary;
		private readonly ILogger      _log;

		public AudioCommandParser(ILogger logger, AudioLibrary audioLibrary)
		{
			_audioLibrary = audioLibrary;
			_log = logger.ForContext<AudioCommandParser>();
		}

		public void Initialize() => _log.Information("Initializing Audio Command Parser");

	#region Parser Combinator

		private static readonly Tokenizer<AudioTokenKind> _tokenizer = new TokenizerBuilder<AudioTokenKind>()
		                                                              .Match(Character.WhiteSpace,                 AudioTokenKind.Delimiter)
		                                                              .Match(Span.EqualTo("a:"),                   AudioTokenKind.AudioAction)
		                                                              .Match(Span.EqualTo("p:"),                   AudioTokenKind.ParamPosition)
		                                                              .Match(Character.EqualTo(','),               AudioTokenKind.Comma)
		                                                              .Match(Character.EqualTo(':'),               AudioTokenKind.ValueDelimiter)
		                                                              .Match(Numerics.Decimal,                     AudioTokenKind.Number)
		                                                              .Match(Span.Regex("[a-zA-Z_][a-zA-Z0-9_]*"), AudioTokenKind.Identifier)
		                                                              .Build();

		private static readonly TokenListParser<AudioTokenKind, string> _text = Token.EqualTo(AudioTokenKind.Identifier).Select(symbol => symbol.ToStringValue());

		private static readonly TokenListParser<AudioTokenKind, float> _float = Token.EqualTo(AudioTokenKind.Number).Select(token => float.Parse(token.ToStringValue()));

		private static readonly TokenListParser<AudioTokenKind, AudioAction> _audioAction =
			from _ in Token.EqualTo(AudioTokenKind.Delimiter)
			               .IgnoreThen(Token.EqualTo(AudioTokenKind.AudioAction).Try())
			from action in Token.EqualToValueIgnoreCase(AudioTokenKind.Identifier, "play")
			                    .Select(_ => AudioAction.Play)
			                    .Try()
			                    .Or(Token.EqualToValueIgnoreCase(AudioTokenKind.Identifier, "toggle").Select(_ => AudioAction.TogglePause))
			                    .Try()
			                    .Or(Token.EqualToValueIgnoreCase(AudioTokenKind.Identifier, "reset").Select(_ => AudioAction.Reset))
			                    .Try()
			                    .Or(Token.EqualToValueIgnoreCase(AudioTokenKind.Identifier, "stop").Select(_ => AudioAction.Stop))
			                    .Try()
			                    .Or(Token.EqualToValueIgnoreCase(AudioTokenKind.Identifier, "remove").Select(_ => AudioAction.Remove))
			                    .Try()
			select action;

		private static readonly TokenListParser<AudioTokenKind, Vector3> _position =
			from _ in Token.EqualTo(AudioTokenKind.Delimiter).IgnoreThen(Token.EqualTo(AudioTokenKind.ParamPosition)).Try()
			from x in _float
			from y in Token.EqualTo(AudioTokenKind.Comma).IgnoreThen(_float)
			from z in Token.EqualTo(AudioTokenKind.Comma).IgnoreThen(_float).Optional()
			select new Vector3(x, y, z.GetValueOrDefault(0));

		private static readonly TokenListParser<AudioTokenKind, AudioParameters> _audioParameters =
			from _ in Token.EqualTo(AudioTokenKind.Delimiter)
			from audioParameters in Token.Sequence(AudioTokenKind.Identifier, AudioTokenKind.ValueDelimiter, AudioTokenKind.Identifier)
			                             .Select(tokens => AudioParameters.WithLabel(tokens[0].ToStringValue(), tokens[^1].ToStringValue()))
			                             .Try()
			                             .Or(Token.Sequence(AudioTokenKind.Identifier, AudioTokenKind.ValueDelimiter, AudioTokenKind.Number)
			                                      .Select(tokens => AudioParameters.WithValue(tokens[0].ToStringValue(), float.Parse(tokens[^1].ToStringValue()))))
			select audioParameters;

		private readonly TokenListParser<AudioTokenKind, AudioCommand> _commandParser =
			from eventReference in _text
			from audioAction in _audioAction.Optional().Try()
			from position in _position.Optional().Try()
			from audioParameters in _audioParameters.Many()
			select BuildCommand(eventReference, audioAction, position, audioParameters);

		private static AudioCommand BuildCommand(string eventReference, AudioAction? audioAction, Vector3? position, AudioParameters[] parameters)
		{
			var command = new AudioCommand { EventName = eventReference };
			if (audioAction.HasValue)
			{
				command.IsOneShot = false;
				command.AudioAction = audioAction.Value;
			}
			else command.IsOneShot = true;

			if (position.HasValue)
				command.Position = position.Value;
			command.Parameters = parameters;
			return command;
		}

		public AudioCommand ParseCommand(in string command)
		{
			var tokens = _tokenizer.Tokenize(command);
			foreach (var token in tokens) _log.Debug("{Token:l}", token);

			return _commandParser.Parse(tokens);
		}

	#endregion
	}
}