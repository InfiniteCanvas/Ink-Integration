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
		public static IContainerBuilder RegisterStoryControllerDependencies(this IContainerBuilder   builder,
		                                                                    InkStoryAsset            inkStoryAsset,
		                                                                    MessagePipeOptions       messagePipeOptions,
		                                                                    ILogger                  logger                   = null,
		                                                                    CommandProcessingOptions commandProcessingOptions = default,
		                                                                    AudioLibrary             audioLibrary             = null,
		                                                                    ImageLibrary             imageLibrary             = null)
		{
			builder.RegisterInstance(inkStoryAsset);

			if (commandProcessingOptions.AudioProcessing)
			{
				if (audioLibrary != null)
				{
					builder.RegisterInstance(audioLibrary);
					builder.RegisterEntryPoint<AudioCommandParser>().As<IAudioCommandParser>();
					builder.RegisterEntryPoint<AudioCommandProcessor>().AsSelf();
				}

				if (builder.Exists(typeof(AudioLibrary)))
				{
					builder.RegisterEntryPoint<AudioCommandParser>().As<IAudioCommandParser>();
					builder.RegisterEntryPoint<AudioCommandProcessor>().AsSelf();
				}
			}

			if (commandProcessingOptions.ImageProcessing)
			{
				if (imageLibrary != null)
				{
					builder.RegisterInstance(imageLibrary);
					builder.RegisterEntryPoint<ImageCommandParser>().As<IImageCommandParser>();
					builder.RegisterEntryPoint<ImageCommandProcessor>().AsSelf();
				}

				if (builder.Exists(typeof(ImageLibrary)))
				{
					builder.RegisterEntryPoint<ImageCommandParser>().As<IImageCommandParser>();
					builder.RegisterEntryPoint<ImageCommandProcessor>().AsSelf();
				}
			}

			if (logger != null)
			{
				builder.RegisterInstance(logger).As<ILogger>();
			}
			else
			{
				builder.Register(resolver =>
				                 {
					                 var injectedLogger = resolver.Resolve<ILogger>();
					                 if (injectedLogger == null)
						                 throw new NullReferenceException("Could not resolve ILogger. Inject it in your LifetimeScope.");

					                 return injectedLogger;
				                 },
				                 Lifetime.Singleton);
			}

			builder.RegisterMessageBroker<ContinueMessage>(messagePipeOptions);
			builder.RegisterMessageBroker<ChoiceMessage>(messagePipeOptions);
			builder.RegisterMessageBroker<ChoiceSelectedMessage>(messagePipeOptions);
			builder.RegisterMessageBroker<CommandMessage>(messagePipeOptions);
			builder.RegisterMessageBroker<TextMessage>(messagePipeOptions);
			builder.RegisterMessageBroker<SaveMessage>(messagePipeOptions);
			builder.RegisterMessageBroker<LoadMessage>(messagePipeOptions);

			builder.RegisterEntryPoint<StoryController>().AsSelf();

			return builder;
		}

		public static MessagePipeOptions RegisterStoryControllerDependencies(this IContainerBuilder     builder,
		                                                                     InkStoryAsset              inkStoryAsset,
		                                                                     ILogger                    logger                   = null,
		                                                                     CommandProcessingOptions   commandProcessingOptions = default,
		                                                                     AudioLibrary               audioLibrary             = null,
		                                                                     ImageLibrary               imageLibrary             = null,
		                                                                     Action<MessagePipeOptions> configure                = null)
		{
			configure ??= _ => { };
			var options = builder.RegisterMessagePipe(configure);

			builder.RegisterInstance(inkStoryAsset);

			if (commandProcessingOptions.AudioProcessing)
			{
				if (audioLibrary != null)
				{
					builder.RegisterInstance(audioLibrary);
					builder.RegisterEntryPoint<AudioCommandParser>().As<IAudioCommandParser>();
					builder.RegisterEntryPoint<AudioCommandProcessor>().AsSelf();
				}

				if (builder.Exists(typeof(AudioLibrary)))
				{
					builder.RegisterEntryPoint<AudioCommandParser>().As<IAudioCommandParser>();
					builder.RegisterEntryPoint<AudioCommandProcessor>().AsSelf();
				}
			}

			if (commandProcessingOptions.ImageProcessing)
			{
				if (imageLibrary != null)
				{
					builder.RegisterInstance(imageLibrary);
					builder.RegisterEntryPoint<ImageCommandParser>().As<IImageCommandParser>();
					builder.RegisterEntryPoint<ImageCommandProcessor>().AsSelf();
				}

				if (builder.Exists(typeof(ImageLibrary)))
				{
					builder.RegisterEntryPoint<ImageCommandParser>().As<IImageCommandParser>();
					builder.RegisterEntryPoint<ImageCommandProcessor>().AsSelf();
				}
			}


			if (logger != null)
			{
				builder.RegisterInstance(logger).As<ILogger>();
			}
			else
			{
				builder.Register(resolver =>
				                 {
					                 var injectedLogger = resolver.Resolve<ILogger>();
					                 if (injectedLogger == null)
						                 throw new NullReferenceException("Could not resolve ILogger. Inject it in your LifetimeScope.");

					                 return injectedLogger;
				                 },
				                 Lifetime.Singleton);
			}

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