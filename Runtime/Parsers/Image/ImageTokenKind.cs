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
}