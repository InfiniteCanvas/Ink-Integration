using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace InfiniteCanvas.InkIntegration.Samples
{
	public class InputListener : MonoBehaviour
	{
		public static bool Maximally;

		[SerializeField] private bool _maximally;

		private IPublisher<ContinueMessage> _continuePublisher;

		private void Awake() => Maximally = _maximally;

		private void Start() => this.InjectStoryControllerDependencies();

		public void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) _continuePublisher.Publish(new ContinueMessage(_maximally));
		}

		[Inject]
		public void Construct(IPublisher<ContinueMessage> continuePublisher) => _continuePublisher = continuePublisher;
	}
}