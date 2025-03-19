using System.IO;
using InfiniteCanvas.InkIntegration.Extensions;
using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;
using VContainer;

namespace InfiniteCanvas.InkIntegration.Samples
{
	public class InputListener : MonoBehaviour
	{
		public static bool Maximally;

	#region Serialized Fields

		[SerializeField] private bool _maximally;

	#endregion

		private IPublisher<ContinueMessage> _continuePublisher;

	#region Event Functions

		private void Awake()
		{
			Maximally = _maximally;
			var absFileName = Path.Combine(Application.dataPath, "InfiniteCanvas.InkIntegration.log");
			Log.Logger = new LoggerConfig().MinimumLevel.Verbose()
			                               .OutputTemplate("[{Timestamp:HH:mm:ss} {Level}] {Message}{NewLine}{StackTrace}")
			                               .WriteTo.UnityDebugLog()
			                               .WriteTo.JsonFile(absFileName)
			                               .CreateLogger();
			Log.Info(absFileName);
		}

		private void Start() => this.InjectStoryControllerDependencies();

		public void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) _continuePublisher.Publish(new ContinueMessage(_maximally));
		}

	#endregion

		[Inject]
		public void Construct(IPublisher<ContinueMessage> continuePublisher) => _continuePublisher = continuePublisher;
	}
}