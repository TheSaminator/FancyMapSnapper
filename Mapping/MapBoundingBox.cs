using SkiaSharp;

namespace FancyMapSnapper.Mapping;

public struct MapBoundingBox {
	public double XMin; // West
	public double YMin; // South
	public double XMax; // East
	public double YMax; // North

	public double Width => XMax - XMin;
	public double Height => YMax - YMin;

	public void Clear() {
		XMin = double.PositiveInfinity;
		XMax = double.NegativeInfinity;
		YMin = double.PositiveInfinity;
		YMax = double.NegativeInfinity;
	}

	public readonly bool IsEmpty => XMin > XMax || YMin > YMax;

	public void Extend(in MapPoint point) {
		if (XMin > point.X)
			XMin = point.X;

		if (XMax < point.X)
			XMax = point.X;

		if (YMin > point.Y)
			YMin = point.Y;

		if (YMax < point.Y)
			YMax = point.Y;
	}

	public readonly SKRect Draw(in SKRect screenBBox) {
		return screenBBox.AspectFit(new SKSize { Width = (float)(XMax - XMin), Height = (float)(YMax - YMin) });
	}
}
