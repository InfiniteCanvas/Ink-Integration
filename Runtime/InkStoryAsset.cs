using UnityEngine;

namespace InfiniteCanvas.InkIntegration
{
	// this exists so we can inject it into vcontainer as its own class, not just using TextAsset
	[CreateAssetMenu(fileName = "Ink Story Asset", menuName = "Infinite Canvas/Ink Story Asset", order = 0)]
	public class InkStoryAsset : ScriptableObject
	{
		public TextAsset InkStoryJson;
	}
}