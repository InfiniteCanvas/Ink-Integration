using System;
using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration.Extensions
{
	public static class LifetimeScopeExtensions
	{
		public static MessagePipeOptions RegisterStoryControllerDependencies(this IContainerBuilder     builder,
		                                                                     InkStoryAsset              inkStoryAsset,
		                                                                     StoryControllerLogSettings logSettings = null,
		                                                                     Action<MessagePipeOptions> configure   = null)
		{
			configure ??= _ => { };
			var options = builder.RegisterMessagePipe(configure);

			logSettings ??= new StoryControllerLogSettings(StoryControllerLogSettings.LogLevel.Debug,
			                                               (_, s) =>
			                                               {
				                                               Debug.Log(s);
			                                               });

			builder.RegisterInstance(logSettings);
			builder.RegisterInstance(inkStoryAsset);

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