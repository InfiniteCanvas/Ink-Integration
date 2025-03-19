using System;
using System.Collections.Generic;
using InfiniteCanvas.InkIntegration.Extensions;
using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace InfiniteCanvas.InkIntegration.Samples
{
	public class ChoicesPresenter : MonoBehaviour
	{
	#region Serialized Fields

		[SerializeField] private UIDocument _uiDocument;

	#endregion

		private readonly List<IDisposable> _disposables = new();

		private IPublisher<ChoiceSelectedMessage> _choiceSelectedPublisher;
		private ListView                          _choicesList;

		private ISubscriber<ChoiceMessage>  _choiceSubscriber;
		private IPublisher<ContinueMessage> _continuePublisher;
		private VisualElement               _root;

	#region Event Functions

		public void Start()
		{
			this.InjectStoryControllerDependencies();
			_root = _uiDocument.rootVisualElement;
			_choicesList = _root.Q<ListView>("choices-list");

			ConfigureListView();

			// Subscribe to choice messages
			_disposables.Add(_choiceSubscriber.Subscribe(HandleChoices));

			// Initially hide choices container
			_root.style.display = DisplayStyle.None;
		}

		private void OnDestroy()
		{
			foreach (var disposable in _disposables) disposable.Dispose();

			_disposables.Clear();
		}

	#endregion

		[Inject]
		public void Construct(ISubscriber<ChoiceMessage>        choiceSubscriber,
		                      IPublisher<ChoiceSelectedMessage> choiceSelectedPublisher,
		                      IPublisher<ContinueMessage>       continuePublisher)
		{
			_continuePublisher = continuePublisher;
			_choiceSubscriber = choiceSubscriber;
			_choiceSelectedPublisher = choiceSelectedPublisher;
		}

		private void ConfigureListView()
		{
			_choicesList.reorderable = false;
			_choicesList.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
			_choicesList.showBorder = false;
		}

		private void HandleChoices(ChoiceMessage message)
		{
			_choicesList.itemsSource = message.Choices;
			_choicesList.makeItem = () => new Button();
			_choicesList.bindItem = (ve, index) =>
			                        {
				                        var button = (Button)ve;
				                        button.text = message.Choices[index].text;

				                        // Clear previous event handlers to prevent duplicates
				                        button.clickable = new Clickable(() => OnChoiceSelected(index));
			                        };

			_choicesList.style.height = _choicesList.fixedItemHeight * message.Choices.Count;
			_root.style.display = DisplayStyle.Flex;
		}

		private void OnChoiceSelected(int index)
		{
			_choiceSelectedPublisher.Publish(index);
			_root.style.display = DisplayStyle.None;
			_continuePublisher.Publish(InputListener.Maximally);
		}
	}
}