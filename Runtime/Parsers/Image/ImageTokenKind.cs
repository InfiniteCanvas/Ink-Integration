using System.Text;
using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	public enum ImageTokenKind
	{
		None,
		Identifier, // Letters, digits, underscores
		Number,
		ValueDelimiter,
		ParameterDelimiter,
		Comma,
		ParamPosition,
		ParamScale,
		ParamScreenSpace,
	}

	public static class ImageTokenKindExtensions
	{
		private static readonly StringBuilder _builder = new();

		private static readonly TextParser<string> _identifier =
			from first in Character.Letter
			from rest in Character.LetterOrDigit.Or(Character.EqualTo('_')).Many() //allows alphanumeric with _
			select new string(_builder.Clear().Append(first).Append(rest).ToString());

		public static Tokenizer<ImageTokenKind> GetTokenizer()
		{
			return new TokenizerBuilder<ImageTokenKind>()
			      .Match(Character.EqualTo(' '), ImageTokenKind.ParameterDelimiter)
			      .Match(Character.EqualTo(':'), ImageTokenKind.ValueDelimiter)
			      .Match(Character.EqualTo(','), ImageTokenKind.Comma)
			      .Match(Numerics.Decimal,       ImageTokenKind.Number)
			      .Match(Span.EqualTo("p:"),     ImageTokenKind.ParamPosition)
			      .Match(Span.EqualTo("s:"),     ImageTokenKind.ParamScale)
			      .Match(Span.EqualTo("ui"),     ImageTokenKind.ParamScreenSpace)
			      .Match(_identifier,            ImageTokenKind.Identifier)
			      .Build();
		}
	}
}