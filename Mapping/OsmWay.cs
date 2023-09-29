namespace FancyMapSnapper.Mapping;

public class OsmWay {
	private static readonly Dictionary<string, OsmWay> Ways = new();

	private OsmWay(string id) {
		Id = id;
		Ways[Id] = this;

		Visibility = VisibilityState.Show;
		Tags = new Dictionary<string, string>();
		Nodes = new List<OsmNode>();
		_bbox.Clear();
	}

	public static OsmWay GetWay(string id) {
		return Ways.TryGetValue(id, out var existing) ? existing : new OsmWay(id);
	}

	public static IEnumerable<string> GetTagsByKey(string key) {
		return Ways.Values.Select(way => way.Tags.TryGetValue(key, out var value) ? value : "")
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Distinct()
			.Order();
	}

	public static IEnumerable<OsmWay> GetWaysByTag(string key, string value) {
		return Ways.Values.Where(way => way.Tags.TryGetValue(key, out var test) && test == value).OrderBy(way => way.Id);
	}

	public static IEnumerable<OsmWay> GetWaysWithoutTag(string key) {
		return Ways.Values.Where(way => !way.Tags.ContainsKey(key)).OrderBy(way => way.Id);
	}

	public string Id { get; }
	public VisibilityState Visibility { get; set; }
	public Dictionary<string, string> Tags { get; }
	public List<OsmNode> Nodes { get; }

	private MapBoundingBox _bbox;
	public ref MapBoundingBox BBox => ref _bbox;

	public bool IsHighway {
		get {
			if (Tags.ContainsKey("highway"))
				return true;

			var firstLocation = Nodes.First().Location;
			var lastLocation = Nodes.Last().Location;
			return double.Hypot(firstLocation.X - lastLocation.X, firstLocation.Y - lastLocation.Y) > 0.001;
		}
	}

	public bool IsPolygon => !IsHighway;

	public MapPoint Center {
		get {
			if (IsHighway) {
				if (Nodes.Count % 2 == 1)
					return Nodes[Nodes.Count / 2].Location;
				var center1 = Nodes[Nodes.Count / 2 - 1].Location;
				var center2 = Nodes[Nodes.Count / 2].Location;
				return new MapPoint { X = (center1.X + center2.X) / 2, Y = (center1.Y + center2.Y) / 2 };
			}

			var numPoints = 0;
			var totalPoint = new MapPoint();
			foreach (var node in Nodes) {
				numPoints++;
				totalPoint.X += node.Location.X;
				totalPoint.Y += node.Location.Y;
			}

			return new MapPoint { X = totalPoint.X / numPoints, Y = totalPoint.Y / numPoints };
		}
	}
}
