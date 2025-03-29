using InfiniteCanvas.InkIntegration.Extensions;
using InfiniteCanvas.SerilogIntegration;
using MessagePipe;
using Serilog;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration.Samples
{
	public sealed class StoryLifetimeScope : LifetimeScope
	{
		public static StoryLifetimeScope Instance;

		public InkStoryAsset InkStoryAsset;

		protected override void Configure(IContainerBuilder builder)
		{
			builder.RegisterBuildCallback(BuildCallback);
			var logger = new LoggerConfiguration().MinimumLevel.Verbose()
			                                      .WriteTo.Unity()
			                                      .CreateLogger();

			_ = builder.RegisterStoryControllerDependencies(InkStoryAsset,
			                                                logger,
			                                                options => options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore);
		}

		private void BuildCallback(IObjectResolver resolver) => Instance = this;
	}

	public static class SampleExtensions
	{
		public static void InjectStoryControllerDependencies(this object obj) => StoryLifetimeScope.Instance.Container.Inject(obj);
	}
}