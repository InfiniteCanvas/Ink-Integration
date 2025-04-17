using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using Superpower;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;
using ILogger = Serilog.ILogger;
using Object = UnityEngine.Object;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	public class ImageCommandProcessor : IDisposable, IInitializable
	{
		private readonly Dictionary<string, SpriteRenderer> _activeSpriteRenderers = new();
		private readonly IDisposable                        _disposable;
		private readonly IImageCommandParser                _imageCommandParser;
		private readonly ImageLibrary                       _imageLibrary;
		private readonly ILogger                            _log;
		private readonly ObjectPool<SpriteRenderer>         _spriteRendererPool;

		public ImageCommandProcessor(ILogger                          logger,
		                             IImageCommandParser              imageCommandParser,
		                             ImageLibrary                     imageLibrary,
		                             ISubscriber<CommandMessage>      commandMessageSubscriber,
		                             IAsyncSubscriber<CommandMessage> commandMessageAsyncSubscriber)
		{
			_log = logger.ForContext<ImageCommandProcessor>();
			_spriteRendererPool = new ObjectPool<SpriteRenderer>(() => new GameObject().AddComponent<SpriteRenderer>(),
			                                                     o => o.gameObject.SetActive(true),
			                                                     o =>
			                                                     {
				                                                     o.gameObject.SetActive(false);
				                                                     o.transform.position = Vector3.zero;
				                                                     o.transform.localScale = Vector3.one;
			                                                     },
			                                                     o =>
			                                                     {
				                                                     if (o == null) return;
				                                                     Object.Destroy(o.gameObject);
			                                                     });
			_imageCommandParser = imageCommandParser;
			_imageLibrary = imageLibrary;
			var bag = DisposableBag.CreateBuilder();
			commandMessageSubscriber.Subscribe(CommandMessageHandler).AddTo(bag);
			commandMessageAsyncSubscriber.Subscribe((message, _) =>
			                                        {
				                                        CommandMessageHandler(message);
				                                        return UniTask.CompletedTask;
			                                        })
			                             .AddTo(bag);
			_disposable = bag.Build();
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
			_log.Debug("Processing Image Command: {ImageCommand}", message.Text);

			try
			{
				var imageCommand = _imageCommandParser.ParseCommand(message.Text);
				_log.Debug("From ParserCombinator: {ImageCommand}", imageCommand);

				_log.Debug("Displaying {ImageNameSpace}:{ImagePose}", imageCommand.Namespace, imageCommand.Pose);
				var sprite = _imageLibrary.GetImage(imageCommand.Namespace, imageCommand.Pose);

				if (_activeSpriteRenderers.TryGetValue(imageCommand.Namespace, out var activeSpriteRenderer))
				{
					_log.Verbose("Sprite [{NewSprite}] spawning and replacing [{OldSprite}]", sprite.name, activeSpriteRenderer.sprite.name);
					if (imageCommand.Pose == "delete")
					{
						_spriteRendererPool.Release(activeSpriteRenderer);
						_activeSpriteRenderers.Remove(imageCommand.Namespace);
					}
					else
					{
						activeSpriteRenderer.sprite = sprite;
						if (imageCommand.ModifyPosition) activeSpriteRenderer.transform.position = imageCommand.Position;
						if (imageCommand.ModifyScale) activeSpriteRenderer.transform.localScale = imageCommand.Scale;
					}
				}
				else
				{
					var instantiatedSpriteRenderer = GetSpriteRenderer(sprite, imageCommand.Scale, imageCommand.Position, imageCommand.IsScreenSpace);
					_log.Verbose("Sprite spawned: {NewSprite}", sprite.name);
					_activeSpriteRenderers[imageCommand.Namespace] = instantiatedSpriteRenderer;
				}
			}
			catch (ParseException e)
			{
				_log.Error(e, "Failed to parse Image Command: {ImageCommand}", message.Text);
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