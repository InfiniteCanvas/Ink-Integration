using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration.Extensions
{
	public static class StoryLifetimeScopeExtensions
	{
		public static void InjectStoryControllerDependencies(this object obj) => StoryLifetimeScope.Instance.Container.Inject(obj);

		public static MessagePipeOptions RegisterStoryControllerDependencies(this IContainerBuilder builder, InkStoryAsset inkStoryAsset)
		{
			var options = builder.RegisterMessagePipe(options =>
			                                          {
				                                          options.InstanceLifetime = InstanceLifetime.Singleton;
				                                          options.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
				                                          options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
				                                          options.RequestHandlerLifetime = InstanceLifetime.Singleton;
			                                          });

			builder.RegisterMessageBroker<ContinueMessage>(options);
			builder.RegisterMessageBroker<ChoiceMessage>(options);
			builder.RegisterMessageBroker<ChoiceSelectedMessage>(options);
			builder.RegisterMessageBroker<CommandMessage>(options);
			builder.RegisterMessageBroker<TextMessage>(options);
			builder.RegisterMessageBroker<SaveMessage>(options);
			builder.RegisterMessageBroker<LoadMessage>(options);

			builder.RegisterInstance(inkStoryAsset);
			builder.RegisterEntryPoint<StoryController>().AsSelf();
			return options;
		}
	}
}