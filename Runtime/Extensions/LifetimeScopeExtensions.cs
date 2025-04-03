using System;
using InfiniteCanvas.InkIntegration.Messages;
using InfiniteCanvas.InkIntegration.Parsers.Audio;
using MessagePipe;
using Serilog;
using Serilog.Core;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration.Extensions
{
	public static class LifetimeScopeExtensions
	{
		public static MessagePipeOptions RegisterStoryControllerDependencies(this IContainerBuilder     builder,
		                                                                     InkStoryAsset              inkStoryAsset,
		                                                                     AudioLibrary               audioLibrary,
		                                                                     Logger                     logger,
		                                                                     Action<MessagePipeOptions> configure = null)
		{
			configure ??= _ => { };
			var options = builder.RegisterMessagePipe(configure);

			builder.RegisterInstance(inkStoryAsset);
			builder.RegisterInstance(audioLibrary);
			builder.RegisterInstance(logger).As<ILogger>();

			builder.RegisterMessageBroker<ContinueMessage>(options);
			builder.RegisterMessageBroker<ChoiceMessage>(options);
			builder.RegisterMessageBroker<ChoiceSelectedMessage>(options);
			builder.RegisterMessageBroker<CommandMessage>(options);
			builder.RegisterMessageBroker<TextMessage>(options);
			builder.RegisterMessageBroker<SaveMessage>(options);
			builder.RegisterMessageBroker<LoadMessage>(options);

			builder.RegisterEntryPoint<StoryController>().AsSelf();
			builder.RegisterEntryPoint<AudioCommandParser>().As<IAudioCommandParser>();
			builder.RegisterEntryPoint<AudioPlayer>().AsSelf();
			return options;
		}
	}
}