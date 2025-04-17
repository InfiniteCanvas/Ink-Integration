using System;
using UnityEngine;

namespace InfiniteCanvas.InkIntegration.Parsers.Image
{
	public interface IImageCommandParser
	{
		[Obsolete("Use `ImageCommand ParseCommand(in string command)` instead.")]
		public bool ParseCommand(in  string             command,
		                         out ReadOnlySpan<char> imageNameSpace,
		                         out ReadOnlySpan<char> imagePose,
		                         out Vector3            position,
		                         out Vector3            scale,
		                         out bool               isScreenSpace);

		public ImageCommand ParseCommand(in string command);
	}

	public class ImageCommand
	{
		public string  Namespace      = string.Empty;
		public string  Pose           = "default";
		public Vector3 Position       = Vector3.zero;
		public Vector3 Scale          = Vector3.one;
		public bool    IsScreenSpace  = false;
		public bool    ModifyPosition = false;
		public bool    ModifyScale    = false;

		public override string ToString()
			=> $"{nameof(Namespace)}: {Namespace}, {nameof(Pose)}: {Pose}, {nameof(Position)}: {Position}, {nameof(Scale)}: {Scale}, {nameof(IsScreenSpace)}: {IsScreenSpace}, {nameof(ModifyPosition)}: {ModifyPosition}, {nameof(ModifyScale)}: {ModifyScale}";
	}
}