using System.Collections.Generic;
using InfiniteCanvas.Pooling;
using Ink.Runtime;
using VContainer;

namespace InfiniteCanvas.InkIntegration.Messages
{
	[Pooled(destroyAction: @"item => item.Reset()"), InjectIgnore]
	public partial class ChoiceMessage
	{
		public List<Choice> Choices;

		private ChoiceMessage() { }

		public ChoiceMessage Initialize(List<Choice> choices)
		{
			Choices = choices;
			return this;
		}

		private void Reset() => Choices.Clear();
	}
}