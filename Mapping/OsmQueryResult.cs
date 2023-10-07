using System.Globalization;
using System.Xml;

namespace FancyMapSnapper.Mapping;

public class OsmQueryResult {
	public List<OsmWay> Ways { get; } = new();

	private MapBoundingBox _bbox;
	public ref MapBoundingBox BBox => ref _bbox;

	public static (List<string> places, OsmQueryResult result) FromXml(XmlDocument doc) {
		XmlElement? root = null;
		foreach (var node in doc.ChildNodes) {
			if (node is not XmlElement { Name: "fms-save" } element) continue;
			root = element;
			break;
		}

		var places = new List<string>();
		var result = new OsmQueryResult();
		foreach (var child in root!.ChildNodes) {
			if (child is not XmlElement childElement) continue;

			if (childElement.Name == "place")
				places.Add(childElement.GetAttribute("name"));

			if (childElement.Name == "bbox") {
				result.BBox.XMin = Convert.ToDouble(childElement.GetAttribute("xMin"), CultureInfo.InvariantCulture);
				result.BBox.XMax = Convert.ToDouble(childElement.GetAttribute("xMax"), CultureInfo.InvariantCulture);
				result.BBox.YMin = Convert.ToDouble(childElement.GetAttribute("yMin"), CultureInfo.InvariantCulture);
				result.BBox.YMax = Convert.ToDouble(childElement.GetAttribute("yMax"), CultureInfo.InvariantCulture);
			}

			if (childElement.Name == "node") {
				var osmNode = OsmNode.GetNode(childElement.GetAttribute("id"));
				var wayId = childElement.GetAttribute("way");
				if (!string.IsNullOrWhiteSpace(wayId))
					osmNode.Way = OsmWay.GetWay(wayId);

				foreach (var nodeChildNode in childElement.ChildNodes) {
					if (nodeChildNode is not XmlElement nodeChild) continue;

					if (nodeChild.Name == "pos") {
						osmNode.Location.X = Convert.ToDouble(nodeChild.GetAttribute("x"), CultureInfo.InvariantCulture);
						osmNode.Location.Y = Convert.ToDouble(nodeChild.GetAttribute("y"), CultureInfo.InvariantCulture);
					}

					if (nodeChild.Name == "tag")
						osmNode.Tags[nodeChild.GetAttribute("k")] = nodeChild.GetAttribute("v");
				}
			}

			if (childElement.Name == "way") {
				var osmWay = OsmWay.GetWay(childElement.GetAttribute("id"));
				osmWay.Visibility = (VisibilityState)Convert.ToInt32(childElement.GetAttribute("visibility"));
				osmWay.Nodes.Clear();

				foreach (var wayChildNode in childElement.ChildNodes) {
					if (wayChildNode is not XmlElement wayChild) continue;

					if (wayChild.Name == "bbox") {
						osmWay.BBox.XMin = Convert.ToDouble(wayChild.GetAttribute("xMin"), CultureInfo.InvariantCulture);
						osmWay.BBox.XMax = Convert.ToDouble(wayChild.GetAttribute("xMax"), CultureInfo.InvariantCulture);
						osmWay.BBox.YMin = Convert.ToDouble(wayChild.GetAttribute("yMin"), CultureInfo.InvariantCulture);
						osmWay.BBox.YMax = Convert.ToDouble(wayChild.GetAttribute("yMax"), CultureInfo.InvariantCulture);
					}

					if (wayChild.Name == "tag")
						osmWay.Tags[wayChild.GetAttribute("k")] = wayChild.GetAttribute("v");

					if (wayChild.Name == "node")
						osmWay.Nodes.Add(OsmNode.GetNode(wayChild.GetAttribute("id")));
				}

				result.Ways.Add(osmWay);
			}
		}

		return (places, result);
	}

	public static XmlDocument ToXml(List<string> places, OsmQueryResult result) {
		var saveDoc = new XmlDocument();
		var fmsSave = saveDoc.CreateElement("fms-save");

		foreach (var place in places) {
			var placeElement = saveDoc.CreateElement("place");
			placeElement.SetAttribute("name", place);
			fmsSave.AppendChild(placeElement);
		}

		var bboxElement = saveDoc.CreateElement("bbox");
		bboxElement.SetAttribute("xMin", result.BBox.XMin.ToString(CultureInfo.InvariantCulture));
		bboxElement.SetAttribute("xMax", result.BBox.XMax.ToString(CultureInfo.InvariantCulture));
		bboxElement.SetAttribute("yMin", result.BBox.YMin.ToString(CultureInfo.InvariantCulture));
		bboxElement.SetAttribute("yMax", result.BBox.YMax.ToString(CultureInfo.InvariantCulture));
		fmsSave.AppendChild(bboxElement);

		var nodes = new SortedSet<string>();
		foreach (var way in result.Ways) {
			var wayElement = saveDoc.CreateElement("way");
			wayElement.SetAttribute("id", way.Id);
			wayElement.SetAttribute("visibility", ((int)way.Visibility).ToString());

			var wayBBoxElement = saveDoc.CreateElement("bbox");
			wayBBoxElement.SetAttribute("xMin", result.BBox.XMin.ToString(CultureInfo.InvariantCulture));
			wayBBoxElement.SetAttribute("xMax", result.BBox.XMax.ToString(CultureInfo.InvariantCulture));
			wayBBoxElement.SetAttribute("yMin", result.BBox.YMin.ToString(CultureInfo.InvariantCulture));
			wayBBoxElement.SetAttribute("yMax", result.BBox.YMax.ToString(CultureInfo.InvariantCulture));
			wayElement.AppendChild(wayBBoxElement);

			foreach (var tag in way.Tags) {
				var tagElement = saveDoc.CreateElement("tag");
				tagElement.SetAttribute("k", tag.Key);
				tagElement.SetAttribute("v", tag.Value);
				wayElement.AppendChild(tagElement);
			}

			foreach (var node in way.Nodes) {
				nodes.Add(node.Id);
				var nodeElement = saveDoc.CreateElement("node");
				nodeElement.SetAttribute("id", node.Id);
				wayElement.AppendChild(nodeElement);
			}

			fmsSave.AppendChild(wayElement);
		}

		foreach (var nodeId in nodes) {
			var node = OsmNode.GetNode(nodeId);
			var nodeElement = saveDoc.CreateElement("node");
			nodeElement.SetAttribute("id", node.Id);
			var way = node.Way;
			if (way != null)
				nodeElement.SetAttribute("way", way.Id);

			var nodePosElement = saveDoc.CreateElement("pos");
			nodePosElement.SetAttribute("x", node.Location.X.ToString(CultureInfo.InvariantCulture));
			nodePosElement.SetAttribute("y", node.Location.Y.ToString(CultureInfo.InvariantCulture));
			nodeElement.AppendChild(nodePosElement);

			foreach (var tag in node.Tags) {
				var tagElement = saveDoc.CreateElement("tag");
				tagElement.SetAttribute("k", tag.Key);
				tagElement.SetAttribute("v", tag.Value);
				nodeElement.AppendChild(tagElement);
			}

			fmsSave.AppendChild(nodeElement);
		}

		saveDoc.AppendChild(fmsSave);
		return saveDoc;
	}
}
