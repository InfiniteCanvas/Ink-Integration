using System;
using InfiniteCanvas.InkIntegration.Messages;
using InfiniteCanvas.InkIntegration.Parsers.Audio;
using InfiniteCanvas.InkIntegration.Parsers.Image;
using MessagePipe;
using Serilog;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration.Extensions
{
	public static class LifetimeScopeExtensions
	{
		public static MessagePipeOptions RegisterStoryControllerDependencies(this IContainerBuilder     builder,
		                                                                     InkStoryAsset              inkStoryAsset,
		                                                                     ILogger                    logger,
		                                                                     CommandProcessingOptions   commandProcessingOptions = default,
		                                                                     AudioLibrary               audioLibrary             = null,
		                                                                     ImageLibrary               imageLibrary             = null,
		                                                                     Action<MessagePipeOptions> configure                = null)
		{
			configure ??= _ => { };
			var options = builder.RegisterMessagePipe(configure);

			builder.RegisterInstance(inkStoryAsset);

			if (audioLibrary != null && commandProcessingOptions.AudioProcessing)
			{
				builder.RegisterInstance(audioLibrary);
				builder.RegisterEntryPoint<AudioCommandParser>().As<IAudioCommandParser>();
				builder.RegisterEntryPoint<AudioCommandProcessor>().AsSelf();
			}

			if (imageLibrary != null && commandProcessingOptions.ImageProcessing)
			{
				builder.RegisterInstance(imageLibrary);
				builder.RegisterEntryPoint<ImageCommandParser>().As<IImageCommandParser>();
				builder.RegisterEntryPoint<ImageCommandProcessor>().AsSelf();
			}

			builder.RegisterInstance(logger).As<ILogger>();

			builder.RegisterMessageBroker<ContinueMessage>(options);
			builder.RegisterMessageBroker<ChoiceMessage>(options);
			builder.RegisterMessageBroker<ChoiceSelectedMessage>(options);
			builder.RegisterMessageBroker<CommandMessage>(options);
			builder.RegisterMessageBroker<TextMessage>(options);
			builder.RegisterMessageBroker<SaveMessage>(options);
			builder.RegisterMessageBroker<LoadMessage>(options);

			builder.RegisterEntryPoint<StoryController>().AsSelf();

			return options;
		}
	}

	public readonly struct CommandProcessingOptions
	{
		public readonly bool ImageProcessing;
		public readonly bool AudioProcessing;

		public CommandProcessingOptions(bool imageProcessing, bool audioProcessing)
		{
			ImageProcessing = imageProcessing;
			AudioProcessing = audioProcessing;
		}
	}
}