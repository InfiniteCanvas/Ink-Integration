using InfiniteCanvas.InkIntegration.Extensions;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace InfiniteCanvas.InkIntegration.Samples
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

			_ = builder.RegisterStoryControllerDependencies(InkStoryAsset, new StoryControllerLogSettings(StoryControllerLogSettings.LogLevel.Debug, (_, s) => Debug.Log(s)),
			                                                options =>
			                                                {
				                                                options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
			                                                });
		}

		private void BuildCallback(IObjectResolver resolver) => Instance = this;
	}

	public static class SampleExtensions
	{
		public static void InjectStoryControllerDependencies(this object obj) => StoryLifetimeScope.Instance.Container.Inject(obj);
	}
}