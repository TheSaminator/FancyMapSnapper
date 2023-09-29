using System.Xml;
using FancyMapSnapper.DataStructures;
using FancyMapSnapper.Mapping;
using FancyMapSnapper.Ui;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

public class LoadingOsmData : ApplicationMode {
	private enum QueryMode {
		BBoxExpansion,
		PopulateLists
	}

	private const int NodeRequestChunkSize = 50;

	private static async Task ProcessRequestedWays(List<Task<XmlDocument>> taskQueue, QueryMode mode, OsmQueryResult results) {
		// Store node queries from ways' lists of nodes
		var nodeIdQueries = new List<Func<XmlDocument, XmlElement>>();

		// The outer loop is used to process nodes after processing ways
		while (taskQueue.Count > 0) {
			// The inner loop is used to process each node or way
			while (taskQueue.Count > 0) {
				var taskIndex = taskQueue.FindIndex(task => task.IsCompleted);
				if (taskIndex < 0) {
					await Task.Yield();
					continue;
				}

				var taskResult = await taskQueue[taskIndex];
				taskQueue.RemoveAt(taskIndex);

				var osmRoot = taskResult["osm"];
				if (osmRoot == null) {
					Console.WriteLine($"Got error document: {taskResult.ToXmlString()}");
					continue;
				}

				// Clear out extra stuff that we don't need
				var noteElement = osmRoot["note"];
				if (noteElement != null) osmRoot.RemoveChild(noteElement);

				var metaElement = osmRoot["meta"];
				if (metaElement != null) osmRoot.RemoveChild(metaElement);

				Console.WriteLine($"Processing result xml: {taskResult.ToXmlString()}");

				// Disambiguate type of response: way
				foreach (var wayObj in osmRoot.GetElementsByTagName("way")) {
					var wayElement = (XmlElement)wayObj;

					var way = OsmWay.GetWay(wayElement.GetAttribute("id"));
					foreach (var nodeObj in wayElement.GetElementsByTagName("nd")) {
						var nodeElement = (XmlElement)nodeObj;
						var nodeId = nodeElement.GetAttribute("ref");
						var node = OsmNode.GetNode(nodeId);
						if (node.Way == way && way.Nodes.Contains(node))
							continue;

						node.Way = way;
						way.Nodes.Add(node);

						// Add node to query list
						nodeIdQueries.Add(xmlDoc => xmlDoc.IdQueryElement("node", nodeId));
					}

					foreach (var tagObj in wayElement.GetElementsByTagName("tag")) {
						var tagElement = (XmlElement)tagObj;
						way.Tags[tagElement.GetAttribute("k")] = tagElement.GetAttribute("v");
					}

					if (mode == QueryMode.PopulateLists) results.Ways.Add(way);
				}

				// Disambiguate type of response: node
				foreach (var nodeObj in osmRoot.GetElementsByTagName("node")) {
					var nodeElement = (XmlElement)nodeObj;

					var nodeId = nodeElement.GetAttribute("id");
					var node = OsmNode.GetNode(nodeId);
					if (!double.TryParse(nodeElement.GetAttribute("lat"), out node.Location.Y)) Console.WriteLine($"Unable to get latitude from node with id {nodeId}");
					if (!double.TryParse(nodeElement.GetAttribute("lon"), out node.Location.X)) Console.WriteLine($"Unable to get longitude from node with id {nodeId}");

					if (!node.Location.IsNowhere) {
						if (mode == QueryMode.BBoxExpansion)
							results.BBox.Extend(in node.Location);
						node.Way?.BBox.Extend(in node.Location);
					}

					foreach (var tagObj in nodeElement.GetElementsByTagName("tag")) {
						var tagElement = (XmlElement)tagObj;
						node.Tags[tagElement.GetAttribute("k")] = tagElement.GetAttribute("v");
					}
				}
			}

			// Then we use the query list to fetch every member node at once
			// In chunks, of course, so we don't have to wait as long
			if (nodeIdQueries.Count == 0) continue;

			while (nodeIdQueries.Count > NodeRequestChunkSize) {
				var chunkPartial = nodeIdQueries.Take(NodeRequestChunkSize).ToList();
				nodeIdQueries.RemoveRange(0, NodeRequestChunkSize);
				var scriptDocPartial = XmlHelper.ConstructOsmQuery((scriptDoc, osmScript) => { scriptDoc.UnionQuery(osmScript, chunkPartial); });
				taskQueue.Add(scriptDocPartial.GetPlaceXml());
			}

			if (nodeIdQueries.Count == 0) continue;

			var chunkRemaining = nodeIdQueries.ToList();
			nodeIdQueries.Clear();
			var scriptDocRemaining = XmlHelper.ConstructOsmQuery((scriptDoc, osmScript) => { scriptDoc.UnionQuery(osmScript, chunkRemaining); });
			taskQueue.Add(scriptDocRemaining.GetPlaceXml());
		}
	}

	private static async Task<OsmQueryResult?> PerformPlacesAndBBoxQuery(IEnumerable<string> places) {
		var taskQueue = places.Select(place => XmlHelper.ConstructOsmQuery(
			(scriptDoc, osmScript) => scriptDoc.NameQuery(osmScript, place)
		).GetPlaceXml()).ToList();

		var result = new OsmQueryResult();
		result.BBox.Clear();

		Console.WriteLine("Phase 1 of loading...");
		await ProcessRequestedWays(taskQueue, QueryMode.BBoxExpansion, result);

		if (result.BBox.IsEmpty)
			return null;

		var wayTags = new Dictionary<string, string>();
		taskQueue.Add(XmlHelper.ConstructOsmQuery(
			(scriptDoc, osmScript) => scriptDoc.BBoxQuery(osmScript, result.BBox, wayTags)
		).GetPlaceXml());

		Console.WriteLine("Phase 2 of loading...");
		await ProcessRequestedWays(taskQueue, QueryMode.PopulateLists, result);

		return result;
	}

	private static async Task<List<OsmWay>> PerformPlacesQuery(IEnumerable<string> places) {
		var result = new OsmQueryResult();
		result.BBox.Clear();

		var taskQueue = places.Select(place => XmlHelper.ConstructOsmQuery(
			(scriptDoc, osmScript) => scriptDoc.NameQuery(osmScript, place)
		).GetPlaceXml()).ToList();

		Console.WriteLine("Phase 2 of loading...");
		await ProcessRequestedWays(taskQueue, QueryMode.PopulateLists, result);

		return result.Ways;
	}

	private static async Task<OsmQueryResult> PerformBBoxQuery(MapBoundingBox bbox) {
		var result = new OsmQueryResult {
			BBox = bbox
		};

		var wayTags = new Dictionary<string, string>();
		var taskQueue = new List<Task<XmlDocument>> {
			XmlHelper.ConstructOsmQuery(
				(scriptDoc, osmScript) => scriptDoc.BBoxQuery(osmScript, result.BBox, wayTags)
			).GetPlaceXml()
		};

		Console.WriteLine("Phase 2 of loading...");
		await ProcessRequestedWays(taskQueue, QueryMode.PopulateLists, result);

		return result;
	}

	public LoadingOsmData(List<string> places) {
		PerformPlacesAndBBoxQuery(places).ContinueWith(task => {
			var taskResult = task.Result;
			FmsApp.Instance.PostAction(() => { _next = taskResult == null ? new InitialPlaceListBuilding(places) : new CustomizingMap(places, taskResult); });
		});
	}

	public LoadingOsmData(List<string> origPlaces, in MapBoundingBox bbox) {
		PerformBBoxQuery(bbox).ContinueWith(task => {
			var taskResult = task.Result;
			FmsApp.Instance.PostAction(() => { _next = new CustomizingMap(origPlaces, taskResult); });
		});
	}

	public LoadingOsmData(IEnumerable<string> origPlaces, IReadOnlyCollection<string> newPlaces, OsmQueryResult result) {
		PerformPlacesQuery(newPlaces).ContinueWith(task => {
			var taskResult = task.Result;
			FmsApp.Instance.PostAction(() => {
				var mapPlaces = new List<string>(origPlaces);
				mapPlaces.AddRange(newPlaces);
				result.Ways.AddRange(taskResult);
				_next = new CustomizingMap(mapPlaces, result);
			});
		});
	}

	public override void HandleInput(in InputEvent input) { }

	private const string LoadingMsg = "Loading...";

	private static readonly MutableString LoadingMsgMutable = new() {
		Chars = LoadingMsg.ToArray(),
		Start = 0,
		Length = LoadingMsg.Length
	};

	private readonly SKPaint _paint = CreatePaint();

	public override void Render(SKCanvas canvas) {
		_paint.Color = ColorScheme.UiTheme[5];
		_paint.SelectFont(new FontSpec { Bold = true });
		_paint.TextSize = 32f;

		canvas.DrawAnchoredText(
			LoadingMsgMutable,
			new SKPoint(800, 450),
			HorizontalAnchor.Middle,
			VerticalAnchor.Middle,
			new TextRenderStyle(),
			_paint,
			out _
		);
	}

	private ApplicationMode? _next;
	public override ApplicationMode NextMode => _next ?? this;
}
