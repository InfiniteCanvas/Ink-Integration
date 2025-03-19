using InfiniteCanvas.InkIntegration.Extensions;
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

			_ = builder.RegisterStoryControllerDependencies(InkStoryAsset);
		}

		private void BuildCallback(IObjectResolver resolver) => Instance = this;
	}
}