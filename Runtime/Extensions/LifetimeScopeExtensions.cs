using System;
using InfiniteCanvas.InkIntegration.Messages;
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
		                                                                     Logger                     logger,
		                                                                     Action<MessagePipeOptions> configure = null)
		{
			configure ??= _ => { };
			var options = builder.RegisterMessagePipe(configure);

			builder.RegisterInstance(inkStoryAsset);
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
}