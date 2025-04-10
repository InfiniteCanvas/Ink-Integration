using System;
using InfiniteCanvas.InkIntegration.Messages;
using InfiniteCanvas.InkIntegration.Parsers.Audio;
using InfiniteCanvas.InkIntegration.Parsers.Image;
using InfiniteCanvas.Utilities;
using InfiniteCanvas.Utilities.Extensions;
using MessagePipe;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using ILogger = Serilog.ILogger;

namespace InfiniteCanvas.InkIntegration.Samples
{
	public class InputListener : MonoBehaviour
	{
		public static bool Maximally;

		[SerializeField] private bool _maximally;

		private IPublisher<ContinueMessage> _continuePublisher;
		private IDisposable                 _disposable;
		private ILogger                     _log;

		private readonly Trigger _publishContinue = new();

		private void Awake() => Maximally = _maximally;

		private void Start() => this.InjectStoryControllerDependencies();

		public void Update()
		{
			if (_publishContinue.TryFire)
			{
				_log.Information("Continue");
				_continuePublisher.Publish(new ContinueMessage(_maximally));
				return;
			}

			if (Input.GetKeyDown(KeyCode.Space))
			{
				_continuePublisher.Publish(new ContinueMessage(_maximally));
				return;
			}
		}

		private void OnDestroy() => _disposable.Dispose();

		[Inject]
		public void Construct(IPublisher<ContinueMessage> continuePublisher,
		                      ISubscriber<CommandMessage> commandSubscriber,
		                      ILogger                     logger)
		{
			_continuePublisher = continuePublisher;
			_disposable = commandSubscriber.Subscribe(CommandHandler);
			_log = logger.ForContext<InputListener>();
		}

		private void CommandHandler(CommandMessage message) { _publishContinue.Prime(); }
	}
}