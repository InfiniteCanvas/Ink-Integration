using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration
{
	public sealed class StoryLifetimeScope : LifetimeScope
	{
		public static StoryLifetimeScope Instance;

	#region Serialized Fields

		public InkStoryAsset InkStoryAsset;

	#endregion

		protected override void Configure(IContainerBuilder builder)
		{
			builder.RegisterBuildCallback(BuildCallback);

			var options = builder.RegisterMessagePipe(options =>
			                                          {
				                                          options.InstanceLifetime = InstanceLifetime.Scoped;
				                                          options.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
				                                          options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
				                                          options.RequestHandlerLifetime = InstanceLifetime.Scoped;
			                                          });

			builder.RegisterMessageBroker<ContinueMessage>(options);
			builder.RegisterMessageBroker<ChoiceMessage>(options);
			builder.RegisterMessageBroker<ChoiceSelectedMessage>(options);
			builder.RegisterMessageBroker<CommandMessage>(options);
			builder.RegisterMessageBroker<TextMessage>(options);
			builder.RegisterMessageBroker<SaveMessage>(options);
			builder.RegisterMessageBroker<LoadMessage>(options);

			builder.RegisterInstance(InkStoryAsset);
			builder.RegisterEntryPoint<StoryController>().AsSelf();
		}

		private void BuildCallback(IObjectResolver resolver) => Instance = this;
	}
}