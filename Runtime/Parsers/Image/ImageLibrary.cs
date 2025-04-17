#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Linq;
using InfiniteCanvas.Utilities;
using Serilog;
using Sirenix.OdinInspector;
using UnityEngine;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	[CreateAssetMenu(fileName = "Image Library", menuName = "Infinite Canvas/Image Library", order = 0)]
	public class ImageLibrary : ScriptableObject
	{
		[SerializeField, ReadOnly] private SerializableNestedDictionary<string, string, Sprite> _runtimeLibrary = new();

		[Required(InfoMessageType.Warning, ErrorMessage = "Provide a default image that will be returned when an image could be loaded.")]
		public Sprite DefaultSprite;

		public Sprite GetImage(string nameSpace, string pose)
		{
			if (_runtimeLibrary.TryGetInnerDictionary(nameSpace, out var poses))
			{
				if (poses.TryGetValue(pose, out var sprite))
				{
					Log.ForContext<ImageLibrary>().Debug("Found image {SpriteName} - {ImageNameSpace}:{ImagePose}", sprite.name, nameSpace, pose);
					return sprite;
				}

				// return default from name space, else our generic default sprite
				return poses.TryGetValue("default", out sprite) ? sprite : DefaultSprite;
			}

			Log.ForContext<ImageLibrary>().Error("Could not find namespace: {ImageNameSpace}", nameSpace);
			return DefaultSprite;
		}

#if UNITY_EDITOR
		[PropertySpace, TitleGroup("Image Library", subtitle: "Editable"), SerializeField, HideLabel,
		 ListDrawerSettings(DefaultExpandedState = true)]
		private SerializableNestedDictionary<string, string, Sprite> _imageLibrary = new();

		[Button, TitleGroup("Image Library"), InfoBox("If you change any values, use the 'Write To Runtime Library' button to apply changes.")]
		private void WriteToRuntimeLibrary()
		{
			_runtimeLibrary.Clear();
			foreach (var (key, dic) in _imageLibrary.GetOuterDictionary())
			{
				foreach (var (pose, sprite) in dic)
				{
					_runtimeLibrary.AddOrUpdate(key, pose, sprite);
				}
			}
		}

		[SerializeField, TitleGroup("Readonly"), ListDrawerSettings(IsReadOnly = true, DefaultExpandedState = true)]
		private string[] _imagePaths;

		[ButtonGroup("Readonly/Tools")]
		public void GetImagePaths(string[] searchInFolders)
		{
			var guids = AssetDatabase.FindAssets("t=Sprite", searchInFolders);
			var spritePaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

			foreach (var path in spritePaths) Debug.Log($"Sprite path: {path}");

			_imagePaths = spritePaths;

			Debug.Log($"Total sprites found: {spritePaths.Length}");
		}

		[ButtonGroup("Readonly/Tools")]
		public void WriteImagePathsToLibrary()
		{
			_runtimeLibrary.Clear();
			foreach (var imagePath in _imagePaths)
			{
				var folder = Directory.GetParent(imagePath)?.Name;
				var filename = Path.GetFileNameWithoutExtension(imagePath);
				var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);

				_runtimeLibrary.AddOrUpdate(folder, filename, sprite);
				_imageLibrary.AddOrUpdate(folder, filename, sprite);
			}
		}
#endif
	}
}