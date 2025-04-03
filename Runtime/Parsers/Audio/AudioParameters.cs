namespace InfiniteCanvas.InkIntegration.Parsers.Audio
{
	public struct AudioParameters
	{
		public string Name;
		public string Label;
		public float  Value;
		public bool   HasLabel;

		public static AudioParameters WithValue(string name, float value) => new() { Name = name, Value = value, HasLabel = false };

		public static AudioParameters WithLabel(string name, string label) => new() { Name = name, Label = label, HasLabel = true };

		public override string ToString() => $"{nameof(Name)}: {Name}, {(HasLabel ? nameof(Label) : nameof(Value))}: {(HasLabel ? Label : Value)}";
	}
}