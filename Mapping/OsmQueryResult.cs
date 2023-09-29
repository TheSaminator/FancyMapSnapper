namespace FancyMapSnapper.Mapping;

public class OsmQueryResult {
	public List<OsmWay> Ways { get; } = new();

	private MapBoundingBox _bbox;
	public ref MapBoundingBox BBox => ref _bbox;
}
