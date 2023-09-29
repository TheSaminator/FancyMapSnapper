using FancyMapSnapper.Ui;
using OpenTK.Mathematics;
using SkiaSharp;

namespace FancyMapSnapper.Mapping;

public static class ArrowRenderer {
	private static void RLineTo(this SKPath path, Vector2 vec) {
		path.RLineTo(vec.X, vec.Y);
	}

	public static SKPath DrawArrow(in SKPoint arrowHead, in SKPoint towardLocation, float arrowWidth, float arrowLength, out SKPoint drawTextAt) {
		// Using OpenTK's Vector2 class makes math a lot easier!
		var toPointUnit = new Vector2 { X = towardLocation.X - arrowHead.X, Y = towardLocation.Y - arrowHead.Y };
		toPointUnit.Normalize();
		var sideToPointUnit = toPointUnit.PerpendicularRight;

		var path = new SKPath();
		path.MoveTo(arrowHead);
		path.RLineTo((-toPointUnit + sideToPointUnit) * arrowWidth / 2);
		path.RLineTo(-sideToPointUnit * arrowWidth / 4);
		path.RLineTo(-toPointUnit * (arrowLength - arrowWidth / 2));
		path.RLineTo(-sideToPointUnit * arrowWidth / 2);
		path.RLineTo(toPointUnit * (arrowLength - arrowWidth / 2));
		path.RLineTo(-sideToPointUnit * arrowWidth / 4);
		path.RLineTo((toPointUnit + sideToPointUnit) * arrowWidth / 2);
		path.Close();

		drawTextAt = new SKPoint { X = arrowHead.X - toPointUnit.X * arrowLength / 2, Y = arrowHead.Y - toPointUnit.Y * arrowLength / 2 };
		return path;
	}

	private static bool Intersects(SKPoint a1, SKPoint a2, SKPoint b1, SKPoint b2, out SKPoint intersection) {
		intersection = new SKPoint { X = 0, Y = 0 };

		var aDiff = a2 - a1;
		var bDiff = b2 - b1;
		var bDotDPerp = aDiff.X * bDiff.Y - aDiff.Y * bDiff.X;

		if (bDotDPerp == 0)
			return false;

		var bToA1 = b1 - a1;
		var t = (bToA1.X * bDiff.Y - bToA1.Y * bDiff.X) / bDotDPerp;
		if (t is < 0 or > 1)
			return false;

		var u = (bToA1.X * aDiff.Y - bToA1.Y * aDiff.X) / bDotDPerp;
		if (u is < 0 or > 1)
			return false;

		intersection = a1 + new SKPoint { X = aDiff.X * t, Y = aDiff.Y * t };

		return true;
	}

	public static bool GetArrowHeadLocation(in MapPoint toLocation, in MapBoundingBox bbox, in SKRect screenBBox, float insetAmount, out SKPoint drawPoint, out SKPoint arrowHeadLocation) {
		drawPoint = toLocation.Draw(in bbox, in screenBBox);
		arrowHeadLocation = drawPoint;
		var innerBBox = screenBBox.Inset(insetAmount);
		if (drawPoint.Y >= innerBBox.Top && drawPoint.X >= innerBBox.Left && drawPoint.X <= innerBBox.Right && drawPoint.Y <= innerBBox.Bottom)
			return false;

		var bboxCenter = new SKPoint { X = innerBBox.MidX, Y = screenBBox.MidY };

		var upperLeft = new SKPoint { X = innerBBox.Left, Y = innerBBox.Top };
		var upperRight = new SKPoint { X = innerBBox.Right, Y = innerBBox.Top };
		var lowerLeft = new SKPoint { X = innerBBox.Left, Y = innerBBox.Bottom };
		var lowerRight = new SKPoint { X = innerBBox.Right, Y = innerBBox.Bottom };

		// Find out which side is intersected by the ray from the rectangle center to the desired location
		if (Intersects(drawPoint, bboxCenter, upperLeft, upperRight, out var topPoint)) {
			arrowHeadLocation = topPoint;
			return true;
		}

		if (Intersects(drawPoint, bboxCenter, lowerLeft, upperLeft, out var leftPoint)) {
			arrowHeadLocation = leftPoint;
			return true;
		}

		if (Intersects(drawPoint, bboxCenter, upperRight, lowerRight, out var rightPoint)) {
			arrowHeadLocation = rightPoint;
			return true;
		}

		if (Intersects(drawPoint, bboxCenter, lowerRight, lowerLeft, out var bottomPoint)) {
			arrowHeadLocation = bottomPoint;
			return true;
		}

		return false;
	}
}
