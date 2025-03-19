namespace InfiniteCanvas.InkIntegration.Extensions
{
	public static class StoryLifetimeScopeExtensions
	{
		public static void InjectStoryControllerDependencies(this object obj) => StoryLifetimeScope.Instance.Container.Inject(obj);
	}
}