using SkiaSharp;

namespace FancyMapSnapper.Mapping;

public struct MapPoint {
	public double X; // Longitude
	public double Y; // Latitude

	public void Clear() {
		X = double.NaN;
		Y = double.NaN;
	}

	public readonly bool IsNowhere => double.IsNaN(X) || double.IsNaN(Y);

	public readonly SKPoint Draw(in MapBoundingBox bbox, in SKRect screenBBox) {
		var bboxOriginPoint = new MapPoint { X = X - bbox.XMin, Y = bbox.YMax - Y };
		var bboxNormalPoint = new SKPoint { X = (float)(bboxOriginPoint.X / (bbox.XMax - bbox.XMin)), Y = (float)(bboxOriginPoint.Y / (bbox.YMax - bbox.YMin)) };
		return new SKPoint {
			X = screenBBox.Left * (1 - bboxNormalPoint.X) + screenBBox.Right * bboxNormalPoint.X,
			Y = screenBBox.Top * (1 - bboxNormalPoint.Y) + screenBBox.Bottom * bboxNormalPoint.Y
		};
	}
}
