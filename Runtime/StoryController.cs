using System;
using System.Runtime.CompilerServices;
using System.Text;
using InfiniteCanvas.InkIntegration.Messages;
using InfiniteCanvas.Utilities.Extensions;
using Ink.Runtime;
using MessagePipe;
using Unity.Logging;
using UnityEngine.Assertions;
using VContainer.Unity;
using File = System.IO.File;

namespace InfiniteCanvas.InkIntegration
{
	public sealed class StoryController : IDisposable, IInitializable
	{
		private readonly IPublisher<ChoiceMessage>  _choicePublisher;
		private readonly IPublisher<CommandMessage> _commandPublisher;
		private readonly IDisposable                _disposable;
		private readonly IPublisher<EndMessage>     _endPublisher;
		private readonly IPublisher<TextMessage>    _textPublisher;

		// I'm exposing this, so I can subscribe to some of the events, if ever needed
		public readonly Story Story;

		public StoryController(InkStoryAsset                      inkStoryAsset,
		                       ISubscriber<ContinueMessage>       continueSubscriber,
		                       ISubscriber<ChoiceSelectedMessage> choiceSelectedSubscriber,
		                       ISubscriber<SaveMessage>           saveMessageSubscriber,
		                       ISubscriber<LoadMessage>           loadMessageSubscriber,
		                       IPublisher<ChoiceMessage>          choicePublisher,
		                       IPublisher<CommandMessage>         commandPublisher,
		                       IPublisher<TextMessage>            textPublisher,
		                       IPublisher<EndMessage>             endPublisher)
		{
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


		public void Initialize()
		{
			using (Log.Decorate("Scope", "StoryController"))
				Log.Info("Story Controller initialized");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private LineType GetLineType(ReadOnlySpan<char> text)
		{
			if (text.Length < 2 || text[0] != '>') return LineType.Text;

			return text[1] switch
			{
				'!' => LineType.Audio,
				'@' => LineType.Animation,
				'~' => LineType.Scene,
				'$' => LineType.UI,
				'>' => LineType.Other,
				_   => LineType.Text,
			};
		}

	#region Handlers & Publishers

		private void ContinueHandler(ContinueMessage message = default)
		{
			if (!Story.canContinue)
			{
				if (!Story.currentChoices.IsNullOrEmpty()) return;
				_endPublisher.Publish(default);
				Log.Info("Story ended.");

				return;
			}

			if (message.Maximally)
			{
				ContinueUntilCommandOrChoice();

				return;
			}

			var text = Story.Continue();

			Assert.IsFalse(string.IsNullOrEmpty(text));

			using (Log.Decorate("Scope", "StoryController"))
			{
				Log.Debug("Raw text: {0}", text);

				var lineType = GetLineType(text);
				if (lineType != LineType.Text)
				{
					var command = text[2..^1];
					_commandPublisher.Publish(new CommandMessage(text: command, lineType: lineType));
					Log.Debug("{1} Command: {0}", command, lineType);
				}
				else
				{
					_textPublisher.Publish(text);
				}


				if (Story.currentChoices.Count > 0) PublishChoices();
			}
		}

		private void ContinueUntilCommandOrChoice()
		{
			var builder = new StringBuilder();
			while (Story.canContinue)
			{
				var part = Story.Continue();
				var lineType = GetLineType(part);
				Log.Debug("Raw text: {0}", part);
				if (lineType == LineType.Text)
				{
					builder.Append(part);
					continue;
				}

				Log.Debug("Sending aggregate text: {0}", builder.ToString());
				_textPublisher.Publish(builder.ToString());
				var command = part[2..^1];
				Log.Debug("{1} Command: {0}", command, lineType);
				_commandPublisher.Publish(new CommandMessage(lineType, command));
				return;
			}

			Log.Debug("Sending aggregate text: {0}", builder.ToString());
			_textPublisher.Publish(builder.ToString());

			if (Story.currentChoices.IsNullOrEmpty())
			{
				Log.Info("Story ended.");
				_endPublisher.Publish(default);
			}
			else
			{
				PublishChoices();
			}
		}


		private void ChoiceSelectedHandler(ChoiceSelectedMessage message)
		{
			Log.Debug("Choice {0} selected", message.Index);
			Story.ChooseChoiceIndex(message.Index);
		}

		private void PublishChoices()
		{
			using (Log.Decorate("Scope", "StoryController"))
			{
				foreach (var choice in Story.currentChoices) Log.Debug("Present choice[{0}]: {1}", choice.index, choice.text);

				using (ChoiceMessage.Get(out var choiceMessage)) _choicePublisher.Publish(choiceMessage.Initialize(Story.currentChoices));
			}
		}

	#endregion
	}
}