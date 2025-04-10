using System;
using System.Collections.Generic;
using InfiniteCanvas.InkIntegration.Messages;
using InfiniteCanvas.Utilities.Extensions;
using MessagePipe;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;
using ILogger = Serilog.ILogger;
using Object = UnityEngine.Object;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	public class ImageCommandProcessor : IDisposable, IInitializable
	{
		private readonly Dictionary<int, SpriteRenderer> _activeSpriteRenderers = new();
		private readonly IDisposable                     _disposable;
		private readonly IImageCommandParser             _imageCommandParser;
		private readonly ImageLibrary                    _imageLibrary;
		private readonly ILogger                         _log;
		private readonly ObjectPool<SpriteRenderer>      _spriteRendererPool;

		public ImageCommandProcessor(ILogger                     logger,
		                             IImageCommandParser         imageCommandParser,
		                             ImageLibrary                imageLibrary,
		                             ISubscriber<CommandMessage> commandMessageSubscriber)
		{
			_log = logger.ForContext<ImageCommandProcessor>();
			_spriteRendererPool = new ObjectPool<SpriteRenderer>(() => new GameObject().AddComponent<SpriteRenderer>(),
			                                                     o => o.gameObject.SetActive(true),
			                                                     o => o.gameObject.SetActive(false),
			                                                     o =>
			                                                     {
				                                                     if (o == null) return;
				                                                     Object.Destroy(o.gameObject);
			                                                     });
			_imageCommandParser = imageCommandParser;
			_imageLibrary = imageLibrary;
			_disposable = commandMessageSubscriber.Subscribe(CommandMessageHandler);
		}

		public void Dispose()
		{
			_disposable?.Dispose();
			foreach (var (_, value) in _activeSpriteRenderers)
			{
				if (value == null)
					continue;

				_spriteRendererPool.Release(value);
			}

			_spriteRendererPool.Dispose();
		}

		public void Initialize() => _log.Information("Initializing Image Command Processor");

		private void CommandMessageHandler(CommandMessage message)
		{
			if (message.CommandType != CommandType.Image) return;

			if (!_imageCommandParser.ParseCommand(message.Text, out var imageNameSpace, out var imagePose, out var position, out var scale, out var isScreenSpace))
			{
				_log.Error("Failed to parse image command: {ImageCommand}", message.Text);
				return;
			}

			_log.Debug("Displaying {ImageNameSpace}:{ImagePose}", imageNameSpace.ToString(), imagePose.ToString());
			var nameSpaceHash = imageNameSpace.GetCustomHashCode();
			var poseHash = imagePose.GetCustomHashCode();
			var sprite = _imageLibrary.GetImage(nameSpaceHash, poseHash);
			var instantiatedSpriteRenderer = GetSpriteRenderer(sprite, scale, position, isScreenSpace);

			if (_activeSpriteRenderers.TryGetValue(nameSpaceHash, out var activeSpriteRenderer))
			{
				_log.Debug("Sprite [{NewSprite}] spawning and replacing [{OldSprite}]", sprite.name, activeSpriteRenderer.sprite.name);
				_spriteRendererPool.Release(activeSpriteRenderer);
				_activeSpriteRenderers[nameSpaceHash] = instantiatedSpriteRenderer;
			}
			else
			{
				_log.Debug("Sprite spawned: {NewSprite}", sprite.name);
				_activeSpriteRenderers[nameSpaceHash] = instantiatedSpriteRenderer;
			}
		}

		private SpriteRenderer GetSpriteRenderer(Sprite sprite, Vector3 scale, Vector3 position, bool isScreenSpace)
		{
			var spriteRenderer = _spriteRendererPool.Get();
			spriteRenderer.gameObject.name = sprite.name;
			spriteRenderer.sprite = sprite;
			spriteRenderer.transform.localScale = scale;
			// TODO: handle screen space images later
			spriteRenderer.transform.position = position;

			return spriteRenderer;
		}
	}
}