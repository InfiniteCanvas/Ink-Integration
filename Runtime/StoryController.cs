using System;
using System.Runtime.CompilerServices;
using System.Text;
using InfiniteCanvas.InkIntegration.Messages;
using Ink.Runtime;
using MessagePipe;
using UnityEngine.Assertions;
using VContainer.Unity;
using File = System.IO.File;
using LogLevel = InfiniteCanvas.InkIntegration.StoryControllerLogSettings.LogLevel;

namespace InfiniteCanvas.InkIntegration
{
	public sealed class StoryController : IDisposable, IInitializable
	{
		private readonly IPublisher<ChoiceMessage>  _choicePublisher;
		private readonly IPublisher<CommandMessage> _commandPublisher;
		private readonly IDisposable                _disposable;
		private readonly IPublisher<EndMessage>     _endPublisher;
		private readonly StoryControllerLogSettings _logSettings;
		private readonly IPublisher<TextMessage>    _textPublisher;

		// I'm exposing this, so I can subscribe to some of the events, if ever needed
		public readonly Story Story;

		public StoryController(InkStoryAsset                      inkStoryAsset,
		                       StoryControllerLogSettings         logSettings,
		                       ISubscriber<ContinueMessage>       continueSubscriber,
		                       ISubscriber<ChoiceSelectedMessage> choiceSelectedSubscriber,
		                       ISubscriber<SaveMessage>           saveMessageSubscriber,
		                       ISubscriber<LoadMessage>           loadMessageSubscriber,
		                       IPublisher<ChoiceMessage>          choicePublisher,
		                       IPublisher<CommandMessage>         commandPublisher,
		                       IPublisher<TextMessage>            textPublisher,
		                       IPublisher<EndMessage>             endPublisher)
		{
			_logSettings = logSettings;
			_textPublisher = textPublisher;
			_endPublisher = endPublisher;
			_commandPublisher = commandPublisher;
			_choicePublisher = choicePublisher;
			Story = new Story(inkStoryAsset.InkStoryJson.text);

			var bag = DisposableBag.CreateBuilder();
			continueSubscriber.Subscribe(ContinueHandler).AddTo(bag);
			choiceSelectedSubscriber.Subscribe(ChoiceSelectedHandler).AddTo(bag);
			saveMessageSubscriber.Subscribe(message => File.WriteAllText(message, Story.state.ToJson())).AddTo(bag);
			loadMessageSubscriber.Subscribe(message => Story.state.LoadJson(message)).AddTo(bag);
			_disposable = bag.Build();
		}

		public void Dispose() => _disposable.Dispose();


		public void Initialize() => _logSettings.LogIf(LogLevel.Info, "Story Controller initialized");

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsCommand(ReadOnlySpan<char> text, out LineType lineType)
		{
			lineType = LineType.None;
			if (text.Length < 2 || text[0] != '>') return false;

			lineType = text[1] switch
			{
				'!' => LineType.Audio,
				'@' => LineType.Animation,
				'~' => LineType.Scene,
				'$' => LineType.UI,
				'%' => LineType.Image,
				':' => LineType.Text,
				'>' => LineType.Other,
				_   => LineType.None,
			};
			if (lineType == LineType.None)
				_logSettings.LogIf(LogLevel.Warning,
				                   "It's an unknown command. While it's not going to crash anything, you should never see this message. "
				                 + "Use '>:' for 'Other' type commands for now and maybe send a pull request for the command type you need. "
				                 + $"Parsed text: {text.ToString()}");

			return true;
		}

	#region Handlers & Publishers

		private void ContinueHandler(ContinueMessage message = default)
		{
			if (_hasBufferedCommand)
			{
				_logSettings.LogIf(LogLevel.Debug, $"{_bufferedCommand.LineType} Command: {_bufferedCommand.Text}");
				_commandPublisher.Publish(_bufferedCommand);
				_hasBufferedCommand = false;
				return;
			}

			if (!Story.canContinue)
			{
				if (Story.currentChoices.Count > 0) return;
				_endPublisher.Publish(default);
				_logSettings.LogIf(LogLevel.Info, "Story ended.");
				return;
			}

			if (message.Maximally)
			{
				ContinueUntilCommandOrChoice();
				return;
			}

			var text = Story.Continue();

			Assert.IsFalse(string.IsNullOrEmpty(text));
			if (string.IsNullOrEmpty(text))
				_logSettings.LogIf(LogLevel.Error, "Text returned is null or empty.");

			_logSettings.LogIf(LogLevel.Verbose, $"Raw text: {text}");

			if (IsCommand(text, out var lineType))
			{
				var command = text[2..^1];
				_commandPublisher.Publish(new CommandMessage(text: command, lineType: lineType));
				_logSettings.LogIf(LogLevel.Debug, $"{lineType} Command: {command}");
			}
			else
			{
				_textPublisher.Publish(text);
			}


			if (Story.currentChoices.Count > 0) PublishChoices();
		}

		private bool           _hasBufferedCommand;
		private CommandMessage _bufferedCommand;

		private void ContinueUntilCommandOrChoice()
		{
			var builder = new StringBuilder();
			while (Story.canContinue)
			{
				var part = Story.Continue();
				_logSettings.LogIf(LogLevel.Verbose, $"Raw text: {part}");
				if (!IsCommand(part, out var lineType))
				{
					builder.Append(part);
					continue;
				}

				if (builder.Length > 0)
				{
					_logSettings.LogIf(LogLevel.Debug, $"Sending aggregate text: {builder}");
					_textPublisher.Publish(builder.ToString());
					var command = part[2..^1];
					_bufferedCommand = new CommandMessage(lineType, command);
					_hasBufferedCommand = true;
				}
				else
				{
					var command = part[2..^1];
					_logSettings.LogIf(LogLevel.Debug, $"{lineType} Command: {command}");
					_commandPublisher.Publish(new CommandMessage(lineType, command));
				}

				return;
			}

			_logSettings.LogIf(LogLevel.Debug, $"Sending aggregate text: {builder}");
			_textPublisher.Publish(builder.ToString());

			if (Story.currentChoices.Count > 0)
			{
				PublishChoices();
			}
			else
			{
				_logSettings.LogIf(LogLevel.Info, "Story ended.");
				_endPublisher.Publish(default);
			}
		}


		private void ChoiceSelectedHandler(ChoiceSelectedMessage message)
		{
			_logSettings.LogIf(LogLevel.Debug, $"Choice {message.Index} selected");
			Story.ChooseChoiceIndex(message.Index);
		}

		private void PublishChoices()
		{
			foreach (var choice in Story.currentChoices)
				_logSettings.LogIf(LogLevel.Debug, $"Present choice[{choice.index}]: {choice.text}");

			using (ChoiceMessage.Get(out var choiceMessage))
				_choicePublisher.Publish(choiceMessage.Initialize(Story.currentChoices));
		}

	#endregion
	}
}