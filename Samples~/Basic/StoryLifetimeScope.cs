using InfiniteCanvas.InkIntegration.Extensions;
using InfiniteCanvas.InkIntegration.Parsers.Audio;
using InfiniteCanvas.InkIntegration.Parsers.Image;
using InfiniteCanvas.SerilogIntegration;
using MessagePipe;
using Serilog;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration.Samples
{
	public sealed class StoryLifetimeScope : LifetimeScope
	{
		public static StoryLifetimeScope Instance;

		public InkStoryAsset       InkStoryAsset;
		public AudioLibrary        AudioLibrary;
		public ImageLibrary        ImageLibrary;
		public LogSettingOverrides LogSettings;

		protected override void Configure(IContainerBuilder builder)
		{
			builder.RegisterBuildCallback(BuildCallback);
			var logger = new LoggerConfiguration().OverrideLogLevels(LogSettings)
			                                      .WriteTo.Unity()
			                                      .CreateLogger();
			Log.Logger = logger;

			_ = builder.RegisterStoryControllerDependencies(InkStoryAsset,
			                                                logger,
			                                                new CommandProcessingOptions(true, true),
			                                                AudioLibrary,
			                                                ImageLibrary,
			                                                options => options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore);
		}

		private void BuildCallback(IObjectResolver resolver) => Instance = this;
	}

	public static class SampleExtensions
	{
		public static void InjectStoryControllerDependencies(this object obj) => StoryLifetimeScope.Instance.Container.Inject(obj);
	}
}