using System;
using UnityEngine;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	public interface IImageCommandParser
	{
		public bool ParseCommand(in  string             command,
		                         out ReadOnlySpan<char> imageNameSpace,
		                         out ReadOnlySpan<char> imagePose,
		                         out Vector3            position,
		                         out Vector3            scale,
		                         out bool               isScreenSpace);
	}
}