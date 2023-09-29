using System.Globalization;
using System.Net.Http.Headers;
using System.Xml;

namespace FancyMapSnapper.Mapping;

public static class XmlHelper {
	private static readonly HttpClient WebClient = new();

	public static string ToXmlString(this XmlDocument doc) {
		var stringWriter = new StringWriter();
		var xmlWriter = new XmlTextWriter(stringWriter);
		foreach (var child in doc.ChildNodes) {
			if (child is XmlElement element)
				element.WriteTo(xmlWriter);
		}

		xmlWriter.Flush();
		return stringWriter.ToString();
	}

	public static XmlElement NameQueryElement(this XmlDocument scriptDoc, string osmType, string place) {
		var nodeQuery = scriptDoc.CreateElement("query");
		nodeQuery.SetAttribute("type", osmType);

		var nodeQueryHasKv = scriptDoc.CreateElement("has-kv");
		nodeQueryHasKv.SetAttribute("k", "name");
		nodeQueryHasKv.SetAttribute("v", place);

		nodeQuery.AppendChild(nodeQueryHasKv);
		return nodeQuery;
	}

	public static XmlElement BBoxQueryElement(this XmlDocument scriptDoc, string osmType, MapBoundingBox bbox, Dictionary<string, string> tags) {
		var nodeQuery = scriptDoc.CreateElement("query");
		nodeQuery.SetAttribute("type", osmType);

		foreach (var (k, v) in tags) {
			var nodeQueryHasKv = scriptDoc.CreateElement("has-kv");
			nodeQueryHasKv.SetAttribute("k", k);
			nodeQueryHasKv.SetAttribute("v", v);
		}

		var nodeQueryBbox = scriptDoc.CreateElement("bbox-query");
		nodeQueryBbox.SetAttribute("n", bbox.YMax.ToString(CultureInfo.InvariantCulture));
		nodeQueryBbox.SetAttribute("e", bbox.XMax.ToString(CultureInfo.InvariantCulture));
		nodeQueryBbox.SetAttribute("s", bbox.YMin.ToString(CultureInfo.InvariantCulture));
		nodeQueryBbox.SetAttribute("w", bbox.XMin.ToString(CultureInfo.InvariantCulture));

		nodeQuery.AppendChild(nodeQueryBbox);
		return nodeQuery;
	}

	public static XmlElement IdQueryElement(this XmlDocument scriptDoc, string osmType, string id) {
		var idQuery = scriptDoc.CreateElement("id-query");
		idQuery.SetAttribute("type", osmType);
		idQuery.SetAttribute("ref", id);

		return idQuery;
	}

	public static XmlDocument ConstructOsmQuery(Action<XmlDocument, XmlElement> query) {
		var scriptDoc = new XmlDocument();
		var osmScript = scriptDoc.CreateElement("osm-script");
		osmScript.SetAttribute("output", "xml");

		query(scriptDoc, osmScript);

		var printElement = scriptDoc.CreateElement("print");
		osmScript.AppendChild(printElement);

		scriptDoc.AppendChild(osmScript);
		return scriptDoc;
	}

	public static void UnionQuery(this XmlDocument scriptDoc, XmlElement osmScript, List<Func<XmlDocument, XmlElement>> queries) {
		var unionElem = scriptDoc.CreateElement("union");
		foreach (var query in queries) {
			unionElem.AppendChild(query(scriptDoc));
		}

		var recurseElement = scriptDoc.CreateElement("recurse");
		recurseElement.SetAttribute("type", "down");
		unionElem.AppendChild(recurseElement);

		osmScript.AppendChild(unionElem);
	}

	public static void NameQuery(this XmlDocument scriptDoc, XmlElement osmScript, string place) {
		scriptDoc.UnionQuery(osmScript, new List<Func<XmlDocument, XmlElement>> {
			xmlDoc => xmlDoc.NameQueryElement("way", place)
		});
	}

	public static void BBoxQuery(this XmlDocument scriptDoc, XmlElement osmScript, MapBoundingBox bbox, Dictionary<string, string> wayTags) {
		scriptDoc.UnionQuery(osmScript, new List<Func<XmlDocument, XmlElement>> {
			xmlDoc => xmlDoc.BBoxQueryElement("way", bbox, wayTags)
		});
	}

	private static readonly SemaphoreSlim WebRequestLimiter = new(8, 8);

	public static async Task<XmlDocument> GetPlaceXml(this XmlDocument scriptDoc) {
		await WebRequestLimiter.WaitAsync();
		var response = await WebClient.PostAsync("https://overpass.kumi.systems/api/interpreter", new StringContent(scriptDoc.ToXmlString(), new MediaTypeHeaderValue("application/xml")));
		var stringData = await response.Content.ReadAsStringAsync();
		WebRequestLimiter.Release();

		var xmlResponse = new XmlDocument();
		xmlResponse.LoadXml(stringData);
		return xmlResponse;
	}
}
