namespace FancyMapSnapper.Mapping;

public class OsmNode {
	private static readonly Dictionary<string, OsmNode> Nodes = new();

	private OsmNode(string id) {
		Id = id;
		Nodes[Id] = this;

		Tags = new Dictionary<string, string>();
		Way = null;
		_location.Clear();
	}

	public static OsmNode GetNode(string id) {
		return Nodes.TryGetValue(id, out var existing) ? existing : new OsmNode(id);
	}

	public string Id { get; }
	public Dictionary<string, string> Tags { get; }
	public OsmWay? Way { get; set; }

	private MapPoint _location;
	public ref MapPoint Location => ref _location;
}
