using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using InfiniteCanvas.InkIntegration.Messages;
using Ink.Runtime;
using MessagePipe;
using VContainer.Unity;
using ILogger = Serilog.ILogger;

namespace InfiniteCanvas.InkIntegration
{
	public sealed class StoryController : IDisposable, IInitializable
	{
		private readonly IAsyncPublisher<ChoiceMessage>  _choiceAsyncPublisher;
		private readonly IPublisher<ChoiceMessage>       _choicePublisher;
		private readonly IAsyncPublisher<CommandMessage> _commandAsyncPublisher;
		private readonly IPublisher<CommandMessage>      _commandPublisher;
		private readonly IDisposable                     _disposable;
		private readonly IAsyncPublisher<EndMessage>     _endAsyncPublisher;
		private readonly IPublisher<EndMessage>          _endPublisher;
		private readonly ILogger                         _log;
		private readonly IAsyncPublisher<TextMessage>    _textAsyncPublisher;
		private readonly IPublisher<TextMessage>         _textPublisher;

		// I'm exposing this, so I can subscribe to some of the events, if ever needed
		public readonly Story Story;

		public StoryController(InkStoryAsset                           inkStoryAsset,
		                       ILogger                                 logger,
		                       ISubscriber<ContinueMessage>            continueSubscriber,
		                       ISubscriber<ChoiceSelectedMessage>      choiceSelectedSubscriber,
		                       ISubscriber<SaveMessage>                saveMessageSubscriber,
		                       ISubscriber<LoadMessage>                loadMessageSubscriber,
		                       IAsyncSubscriber<ContinueMessage>       continueAsyncSubscriber,
		                       IAsyncSubscriber<ChoiceSelectedMessage> choiceSelectedAsyncSubscriber,
		                       IAsyncSubscriber<SaveMessage>           saveAsyncSubscriber,
		                       IAsyncSubscriber<LoadMessage>           loadAsyncSubscriber,
		                       IPublisher<ChoiceMessage>               choicePublisher,
		                       IPublisher<CommandMessage>              commandPublisher,
		                       IPublisher<TextMessage>                 textPublisher,
		                       IPublisher<EndMessage>                  endPublisher,
		                       IAsyncPublisher<ChoiceMessage>          choiceAsyncPublisher,
		                       IAsyncPublisher<CommandMessage>         commandAsyncPublisher,
		                       IAsyncPublisher<TextMessage>            textAsyncPublisher,
		                       IAsyncPublisher<EndMessage>             endAsyncPublisher)
		{
			_endAsyncPublisher = endAsyncPublisher;
			_textAsyncPublisher = textAsyncPublisher;
			_commandAsyncPublisher = commandAsyncPublisher;
			_choiceAsyncPublisher = choiceAsyncPublisher;
			_textPublisher = textPublisher;
			_endPublisher = endPublisher;
			_commandPublisher = commandPublisher;
			_choicePublisher = choicePublisher;
			_log = logger.ForContext<StoryController>();
			Story = new Story(inkStoryAsset.InkStoryJson.text);

			var bag = DisposableBag.CreateBuilder();

			continueSubscriber.Subscribe(ContinueHandler).AddTo(bag);
			choiceSelectedSubscriber.Subscribe(ChoiceSelectedHandler).AddTo(bag);
			saveMessageSubscriber.Subscribe(message => File.WriteAllText(message, Story.state.ToJson())).AddTo(bag);
			loadMessageSubscriber.Subscribe(message => Story.state.LoadJson(message)).AddTo(bag);
			continueAsyncSubscriber.Subscribe(ContinueHandlerAsync)
			                       .AddTo(bag);
			choiceSelectedAsyncSubscriber.Subscribe((message, _) =>
			                                        {
				                                        ChoiceSelectedHandler(message);
				                                        return UniTask.CompletedTask;
			                                        })
			                             .AddTo(bag);
			saveAsyncSubscriber.Subscribe((message, _) =>
			                              {
				                              File.WriteAllText(message, Story.state.ToJson());
				                              return UniTask.CompletedTask;
			                              })
			                   .AddTo(bag);
			loadAsyncSubscriber.Subscribe((message, _) =>
			                              {
				                              Story.state.LoadJson(message);
				                              return UniTask.CompletedTask;
			                              })
			                   .AddTo(bag);

			_disposable = bag.Build();
		}

		public string CurrentSpeaker { get; private set; }


		public void Dispose() => _disposable.Dispose();

		public void Initialize() => _log.Information("Story Controller initialized");

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsCommand(ReadOnlySpan<char> text, out CommandType commandType)
		{
			commandType = CommandType.None;
			if (text.Length < 2 || text[0] != '>') return false;

			commandType = text[1] switch
			{
				'!' => CommandType.Audio,
				'@' => CommandType.Animation,
				'~' => CommandType.Scene,
				'$' => CommandType.UI,
				':' => CommandType.Image,
				'%' => CommandType.Text,
				'>' => CommandType.Other,
				_   => CommandType.None,
			};
			if (commandType == CommandType.None)
			{
				_log.Warning("It's an unknown command. While it's not going to crash anything, you should never see this message. "
				           + "Use '>:' for 'Other' type commands for now and maybe send a pull request for the command type you need. "
				           + "Parsed text: {RawText:l}",
				             text.ToString());
			}

			return true;
		}

	#region Handlers & Publishers

		private void ContinueHandler(ContinueMessage message = default)
		{
			if (_hasBufferedCommand)
			{
				_log.Debug("{CommandType} Command: {CommandText:l}", _bufferedCommand.CommandType, _bufferedCommand.Text);
				_commandPublisher.Publish(_bufferedCommand);
				_hasBufferedCommand = false;
				return;
			}

			if (!Story.canContinue)
			{
				if (Story.currentChoices.Count > 0) return;
				_endPublisher.Publish(default);
				_log.Information("Story ended");
				return;
			}

			if (message.Maximally)
			{
				ContinueUntilCommandOrChoice();
				return;
			}

			var text = Story.Continue();

			if (string.IsNullOrEmpty(text))
			{
				_log.Error("Text returned is null or empty");
				return;
			}

			_log.Verbose("Raw text: {RawText:l}", text);

			if (IsCommand(text, out var lineType))
			{
				var command = text[2..^1];
				_log.Debug("{CommandType} Command: {CommandText:l}", lineType, command);
				_commandPublisher.Publish(new CommandMessage(text: command, commandType: lineType));
			}
			else
			{
				_log.Information("Text: {Text:l}, {Tags}", text, Story.currentTags);
				if (Story.currentTags.Count > 0)
				{
					CurrentSpeaker = Story.currentTags[0];
					_textPublisher.Publish(TextMessage.WithTag(text));
				}
				else _textPublisher.Publish(text);
			}


			if (Story.currentChoices.Count > 0) PublishChoices();
		}

		private async UniTask ContinueHandlerAsync(ContinueMessage message = default, CancellationToken cancellationToken = default)
		{
			if (_hasBufferedCommand)
			{
				_log.Debug("{CommandType} Command: {CommandText:l}", _bufferedCommand.CommandType, _bufferedCommand.Text);
				await _commandAsyncPublisher.PublishAsync(_bufferedCommand, cancellationToken);
				_hasBufferedCommand = false;
				return;
			}

			if (!Story.canContinue)
			{
				if (Story.currentChoices.Count > 0) return;
				await _endAsyncPublisher.PublishAsync(default, cancellationToken);
				_log.Information("Story ended");
				return;
			}

			if (message.Maximally)
			{
				await ContinueUntilCommandOrChoiceAsync(cancellationToken);
				return;
			}

			var text = Story.Continue();

			if (string.IsNullOrEmpty(text))
			{
				_log.Error("Text returned is null or empty");
				return;
			}

			_log.Verbose("Raw text: {RawText:l}", text);

			if (IsCommand(text, out var lineType))
			{
				var command = text[2..^1];
				_log.Debug("{CommandType} Command: {CommandText:l}", lineType, command);
				await _commandAsyncPublisher.PublishAsync(new CommandMessage(text: command, commandType: lineType), cancellationToken);
			}
			else
			{
				_log.Information("Text: {Text:l}", text);
				if (Story.currentTags.Count > 0)
				{
					CurrentSpeaker = Story.currentTags[0];
					await _textAsyncPublisher.PublishAsync(TextMessage.WithTag(text), cancellationToken);
				}
				else await _textAsyncPublisher.PublishAsync(text, cancellationToken);
			}


			if (Story.currentChoices.Count > 0) await PublishChoicesAsync(cancellationToken);
		}

		private bool           _hasBufferedCommand;
		private CommandMessage _bufferedCommand;

		private void ContinueUntilCommandOrChoice()
		{
			var builder = new StringBuilder();
			while (Story.canContinue)
			{
				var part = Story.Continue();
				_log.Verbose("Raw text: {RawText:l}", part);
				if (!IsCommand(part, out var lineType))
				{
					builder.Append(part);
					continue;
				}

				if (builder.Length > 0)
				{
					_log.Debug("Sending aggregate text: {Text:l}", builder);
					if (Story.currentTags.Count > 0)
					{
						CurrentSpeaker = Story.currentTags[0];
						_textPublisher.Publish(TextMessage.WithTag(builder.ToString()));
					}
					else _textPublisher.Publish(builder.ToString());

					var command = part[2..^1];
					_bufferedCommand = new CommandMessage(lineType, command);
					_hasBufferedCommand = true;
				}
				else
				{
					var command = part[2..^1];
					_log.Debug("{CommandType} Command: {CommandText:l}", lineType, command);
					_commandPublisher.Publish(new CommandMessage(lineType, command));
				}

				return;
			}

			_log.Information("Sending aggregate text: {Text:l}", builder);

			if (Story.currentTags.Count > 0)
			{
				CurrentSpeaker = Story.currentTags[0];
				_textPublisher.Publish(TextMessage.WithTag(builder.ToString()));
			}
			else _textPublisher.Publish(builder.ToString());

			if (Story.currentChoices.Count > 0) PublishChoices();
			else
			{
				_log.Information("Story ended");
				_endPublisher.Publish(default);
			}
		}

		private async UniTask ContinueUntilCommandOrChoiceAsync(CancellationToken cancellationToken = default)
		{
			var builder = new StringBuilder();
			while (Story.canContinue)
			{
				var part = Story.Continue();
				_log.Verbose("Raw text: {RawText:l}", part);
				if (!IsCommand(part, out var lineType))
				{
					builder.Append(part);
					continue;
				}

				if (builder.Length > 0)
				{
					_log.Debug("Sending aggregate text: {Text:l}", builder);
					if (Story.currentTags.Count > 0)
					{
						CurrentSpeaker = Story.currentTags[0];
						await _textAsyncPublisher.PublishAsync(TextMessage.WithTag(builder.ToString()), cancellationToken);
					}
					else await _textAsyncPublisher.PublishAsync(builder.ToString(), cancellationToken);

					var command = part[2..^1];
					_bufferedCommand = new CommandMessage(lineType, command);
					_hasBufferedCommand = true;
				}
				else
				{
					var command = part[2..^1];
					_log.Debug("{CommandType} Command: {CommandText:l}", lineType, command);
					await _commandAsyncPublisher.PublishAsync(new CommandMessage(lineType, command), cancellationToken);
				}

				return;
			}

			_log.Information("Sending aggregate text: {Text:l}", builder);
			if (Story.currentTags.Count > 0)
			{
				CurrentSpeaker = Story.currentTags[0];
				await _textAsyncPublisher.PublishAsync(TextMessage.WithTag(builder.ToString()), cancellationToken);
			}
			else await _textAsyncPublisher.PublishAsync(builder.ToString(), cancellationToken);

			if (Story.currentChoices.Count > 0) await PublishChoicesAsync(cancellationToken);
			else
			{
				_log.Information("Story ended");
				await _endAsyncPublisher.PublishAsync(default, cancellationToken);
			}
		}

		private void ChoiceSelectedHandler(ChoiceSelectedMessage message)
		{
			_log.Information("Choice {ChoiceSelected} selected", message.Index);
			Story.ChooseChoiceIndex(message.Index);
		}

		private void PublishChoices()
		{
			foreach (var choice in Story.currentChoices)
				_log.Information("Present choice[{ChoiceIndex}]: {ChoiceText:l}", choice.index, choice.text);

			using (ChoiceMessage.Get(out var choiceMessage))
				_choicePublisher.Publish(choiceMessage.Initialize(Story.currentChoices));
		}

		private async UniTask PublishChoicesAsync(CancellationToken cancellationToken = default)
		{
			foreach (var choice in Story.currentChoices)
				_log.Information("Present choice[{ChoiceIndex}]: {ChoiceText:l}", choice.index, choice.text);

			using (ChoiceMessage.Get(out var choiceMessage))
				await _choiceAsyncPublisher.PublishAsync(choiceMessage.Initialize(Story.currentChoices), cancellationToken);
		}

	#endregion
	}
}