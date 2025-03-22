using System;
using System.Collections.Generic;
using InfiniteCanvas.InkIntegration.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace InfiniteCanvas.InkIntegration.Samples
{
	public class TextPresenter : MonoBehaviour
	{
	#region Serialized Fields

		[SerializeField] private UIDocument _uiDocument;
		[SerializeField] private bool       _accumulateHistory = true;
		[SerializeField] private int        _maxHistoryLines   = 50;

	#endregion

		private readonly List<IDisposable> _disposables = new();
		private readonly List<string>      _textHistory = new();
		private          ScrollView        _scrollView;

		private Label _textContent;

		private ISubscriber<TextMessage> _textSubscriber;

	#region Event Functions

		private void Start()
		{
			this.InjectStoryControllerDependencies();
			InitializeUI();
			_disposables.Add(_textSubscriber.Subscribe(HandleText));
		}

		private void OnDestroy()
		{
			foreach (var disposable in _disposables) disposable?.Dispose();

			_disposables.Clear();
		}

	#endregion

		[Inject]
		public void Construct(ISubscriber<TextMessage> textSubscriber) => _textSubscriber = textSubscriber;

		private void InitializeUI()
		{
			var root = _uiDocument.rootVisualElement;
			_textContent = root.Q<Label>("dialogue-text");
			_scrollView = root.Q<ScrollView>("dialogue-scroll");
		}

		private void HandleText(TextMessage message)
		{
			if (string.IsNullOrEmpty(message.Text))
				return;

			if (_accumulateHistory)
			{
				_textHistory.Add(message.Text);

				// Trim history if it exceeds the maximum
				while (_textHistory.Count > _maxHistoryLines) _textHistory.RemoveAt(0);

				_textContent.text = string.Join(string.Empty, _textHistory);
			}
			else
			{
				_textContent.text = message.Text;
			}

			// Wait for layout to update before scrolling
			_scrollView.schedule.Execute(() =>
			                             {
				                             _scrollView.scrollOffset = new Vector2(0, _scrollView.contentContainer.worldBound.height);
			                             })
			           .StartingIn(10);
		}
	}
}