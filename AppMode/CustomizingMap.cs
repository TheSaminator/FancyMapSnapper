using System.Diagnostics;
using System.Text;
using FancyMapSnapper.DataStructures;
using FancyMapSnapper.Mapping;
using FancyMapSnapper.Ui;
using FancyMapSnapper.Ui.Widgets;
using SkiaSharp;

namespace FancyMapSnapper.AppMode;

public class CustomizingMap : ApplicationMode {
	private readonly List<string> _places;
	private readonly OsmQueryResult _queryResult;

	private const float ToggleCheckBoxHeight = 64;

	public CustomizingMap(List<string> places, OsmQueryResult queryResult) {
		_places = places;
		_queryResult = queryResult;
	}

	private readonly UiRoot _ui = new();

	private static readonly ColorScheme ShowColors = new(ColorStrength.One, ColorStrength.Three, ColorStrength.One);
	private static readonly ColorScheme NoLabelColors = new(ColorStrength.Three, ColorStrength.Three, ColorStrength.One);
	private static readonly ColorScheme HideColors = new(ColorStrength.Three, ColorStrength.One, ColorStrength.One);

	private readonly UiButton _expandNorth = new() { Size = new SKRect(536, 16, 664, 64), Text = "^" };
	private readonly UiButton _contractNorth = new() { Size = new SKRect(536, 80, 664, 128), Text = "v" };
	private readonly UiButton _expandWest = new() { Size = new SKRect(16, 386, 64, 514), Text = "<" };
	private readonly UiButton _contractWest = new() { Size = new SKRect(80, 386, 128, 514), Text = ">" };
	private readonly UiButton _contractEast = new() { Size = new SKRect(1072, 386, 1120, 514), Text = "<" };
	private readonly UiButton _expandEast = new() { Size = new SKRect(1136, 386, 1184, 514), Text = ">" };
	private readonly UiButton _contractSouth = new() { Size = new SKRect(536, 772, 664, 820), Text = "^" };
	private readonly UiButton _expandSouth = new() { Size = new SKRect(536, 836, 664, 884), Text = "v" };
	private readonly UiButton _refreshBBox = new() { Size = new SKRect(16, 836, 496, 884), Text = "Repopulate Bounding Box" };
	private readonly UiButton _addNewMapObj = new() { Size = new SKRect(416, 772, 496, 820), Text = "Add" };
	private readonly UiInputField _newMapObjName = new() { Size = new SKRect(16, 772, 400, 820) };

	private readonly UiButton _showAll = new() { Size = new SKRect(1216, 16, 1328, 48), Text = "Show All", FontSize = 14, CustomColors = ShowColors };
	private readonly UiButton _showAreas = new() { Size = new SKRect(1216, 64, 1328, 96), Text = "Show Areas", FontSize = 14, CustomColors = ShowColors };
	private readonly UiButton _showPaths = new() { Size = new SKRect(1216, 112, 1328, 144), Text = "Show Paths", FontSize = 14, CustomColors = ShowColors };
	private readonly UiButton _unLabelAll = new() { Size = new SKRect(1344, 16, 1456, 48), Text = "Un-Label All", FontSize = 14, CustomColors = NoLabelColors };
	private readonly UiButton _unLabelAreas = new() { Size = new SKRect(1344, 64, 1456, 96), Text = "Un-Label Areas", FontSize = 14, CustomColors = NoLabelColors };
	private readonly UiButton _unLabelPaths = new() { Size = new SKRect(1344, 112, 1456, 144), Text = "Un-Label Paths", FontSize = 14, CustomColors = NoLabelColors };
	private readonly UiButton _hideAll = new() { Size = new SKRect(1472, 16, 1584, 48), Text = "Hide All", FontSize = 14, CustomColors = HideColors };
	private readonly UiButton _hideAreas = new() { Size = new SKRect(1472, 64, 1584, 96), Text = "Hide Areas", FontSize = 14, CustomColors = HideColors };
	private readonly UiButton _hidePaths = new() { Size = new SKRect(1472, 112, 1584, 144), Text = "Hide Paths", FontSize = 14, CustomColors = HideColors };
	private readonly UiScrollPane _toggles = new() { OuterSize = new SKRect(1200, 160, 1584, 820) };
	private readonly Dictionary<RadioButtonGroup, List<OsmWay>> _associatedWays = new();

	private readonly UiButton _exportMap = new() { Size = new SKRect(1216, 836, 1392, 884), Text = "Export" };
	private readonly UiButton _closeMap = new() { Size = new SKRect(1408, 836, 1584, 884), Text = "Close" };

	private void RepopulateBBox() {
		_next = new LoadingOsmData(_places, in _queryResult.BBox);
	}

	private void AddNewMapObject(StringBuilder name) {
		var inputText = name.ToString();
		if (inputText.StartsWith("UH "))
			inputText = "University Hospitals " + inputText[3..];
		_next = new LoadingOsmData(_places, new List<string> { inputText }, _queryResult);
	}

	private void ShowAll() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			((UiRadioButton)uiGroup.ChildAt(0)).Group.Value = 0;
		}
	}

	private void ShowAreas() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			var radioButtonGroup = ((UiRadioButton)uiGroup.ChildAt(0)).Group;
			var ways = _associatedWays[radioButtonGroup];
			if (ways.Any(way => way.IsPolygon))
				radioButtonGroup.Value = 0;
		}
	}

	private void ShowPaths() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			var radioButtonGroup = ((UiRadioButton)uiGroup.ChildAt(0)).Group;
			var ways = _associatedWays[radioButtonGroup];
			if (ways.Any(way => way.IsHighway))
				radioButtonGroup.Value = 0;
		}
	}

	private void UnLabelAll() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			((UiRadioButton)uiGroup.ChildAt(0)).Group.Value = 1;
		}
	}

	private void UnLabelAreas() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			var radioButtonGroup = ((UiRadioButton)uiGroup.ChildAt(0)).Group;
			var ways = _associatedWays[radioButtonGroup];
			if (ways.Any(way => way.IsPolygon))
				radioButtonGroup.Value = 1;
		}
	}

	private void UnLabelPaths() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			var radioButtonGroup = ((UiRadioButton)uiGroup.ChildAt(0)).Group;
			var ways = _associatedWays[radioButtonGroup];
			if (ways.Any(way => way.IsHighway))
				radioButtonGroup.Value = 1;
		}
	}

	private void HideAll() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			((UiRadioButton)uiGroup.ChildAt(0)).Group.Value = 2;
		}
	}

	private void HideAreas() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			var radioButtonGroup = ((UiRadioButton)uiGroup.ChildAt(0)).Group;
			var ways = _associatedWays[radioButtonGroup];
			if (ways.Any(way => way.IsPolygon))
				radioButtonGroup.Value = 2;
		}
	}

	private void HidePaths() {
		for (var i = 0; i < _toggles.NumChildren; i++) {
			var uiGroup = (UiGroup)_toggles.ChildAt(i);
			var radioButtonGroup = ((UiRadioButton)uiGroup.ChildAt(0)).Group;
			var ways = _associatedWays[radioButtonGroup];
			if (ways.Any(way => way.IsHighway))
				radioButtonGroup.Value = 2;
		}
	}

	private const int MaxImageSize = 2560;
	private const float ImageTextSize = 24;

	private static double GetTextScaling(OsmQueryResult result, in SKRect targetRect) {
		var bboxWidth = result.BBox.XMax - result.BBox.XMin;
		var bboxHeight = result.BBox.YMax - result.BBox.YMin;
		int width;
		if (bboxWidth > bboxHeight)
			width = MaxImageSize;
		else
			width = (int)double.Round(MaxImageSize * (bboxWidth / bboxHeight));

		var screenBbox = result.BBox.Draw(in targetRect);

		return screenBbox.Width / width;
	}

	private static readonly Lazy<DirectoryInfo> ExportDirLazy = new(() => Directory.Exists("exports") ? new DirectoryInfo("exports") : Directory.CreateDirectory("exports"));
	private static DirectoryInfo ExportDir => ExportDirLazy.Value;

	private void ExportMap() {
		var bboxWidth = _queryResult.BBox.XMax - _queryResult.BBox.XMin;
		var bboxHeight = _queryResult.BBox.YMax - _queryResult.BBox.YMin;
		int width, height;
		if (bboxWidth > bboxHeight) {
			width = MaxImageSize;
			height = (int)double.Round(width * (bboxHeight / bboxWidth));
		}
		else {
			height = MaxImageSize;
			width = (int)double.Round(height * (bboxWidth / bboxHeight));
		}

		var surfaceRect = SKRect.Create(width, height);
		using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
		var canvas = surface.Canvas;

		_linePaint.StrokeWidth = 1;
		_roadPaint.StrokeWidth = 3;
		_footwayPaint.StrokeWidth = 2;
		_textPaint.TextSize = ImageTextSize;
		DrawMap(canvas, _queryResult, in surfaceRect, _bgPaint, _areaPaint, _waterPaint, _roadPaint, _footwayPaint, _linePaint, _textPaint, _arrowPaint, ArrowMargin, ArrowWidth, ArrowLength);
		canvas.Flush();

		var img = surface.Snapshot();
		var pngData = img.Encode(SKEncodedImageFormat.Png, 100);
		var fileTime = DateTime.Now;
		var fileName = $"{fileTime.Year:D4}-{fileTime.Month:D2}-{fileTime.Day:D2}T{fileTime.Hour:D2}-{fileTime.Minute:D2}-{fileTime.Second:D2}.{fileTime.Millisecond:D3}{fileTime.Microsecond:D3}.png";

		var exportFile = new FileInfo(Path.Join(ExportDir.FullName, fileName));
		using var fileStream = exportFile.OpenWrite();
		pngData.SaveTo(fileStream);
		Process.Start(new ProcessStartInfo(ExportDir.FullName) { UseShellExecute = true });
	}

	private void CloseMap() {
		_next = new InitialPlaceListBuilding(_places);
	}

	private static readonly double BBoxIncrement = Math.Pow(2, -12);

	private void RefreshBBoxButtons() {
		_contractWest.IsEnabled = _queryResult.BBox.Width > BBoxIncrement;
		_contractEast.IsEnabled = _queryResult.BBox.Width > BBoxIncrement;
		_contractNorth.IsEnabled = _queryResult.BBox.Height > BBoxIncrement;
		_contractSouth.IsEnabled = _queryResult.BBox.Height > BBoxIncrement;
	}

	private void AddWayItem(List<OsmWay> ways, string name, ref float y) {
		if (ways.Count == 0)
			return;

		var maxVisibility = ways.Max(way => way.Visibility);

		var radioButtonGroup = new RadioButtonGroup((int)maxVisibility);
		foreach (var way in ways)
			way.Visibility = maxVisibility;

		radioButtonGroup.OnChange += (_, visibilityIndex) => {
			foreach (var way in ways)
				way.Visibility = (VisibilityState)visibilityIndex;
		};

		var showButton = new UiRadioButton(radioButtonGroup, (int)VisibilityState.Show) {
			HAnchor = HorizontalAnchor.Left,
			AnchorPoint = new SKPoint(8, y),
			CustomColors = new ColorScheme(ColorStrength.One, ColorStrength.Three, ColorStrength.One)
		};
		var unLabelButton = new UiRadioButton(radioButtonGroup, (int)VisibilityState.NoLabel) {
			HAnchor = HorizontalAnchor.Left,
			AnchorPoint = new SKPoint(48, y),
			CustomColors = new ColorScheme(ColorStrength.Three, ColorStrength.Three, ColorStrength.One)
		};
		var hideButton = new UiRadioButton(radioButtonGroup, (int)VisibilityState.Hide) {
			HAnchor = HorizontalAnchor.Left,
			AnchorPoint = new SKPoint(88, y),
			CustomColors = new ColorScheme(ColorStrength.Three, ColorStrength.One, ColorStrength.One)
		};
		var label = new UiLabel {
			HAnchor = HorizontalAnchor.Left,
			AnchorPoint = new SKPoint(128, y),
			Text = name,
			Style = new TextStyle { Color = ColorScheme.UiTheme[5] }
		};

		var group = new UiGroup();
		group.AddChild(showButton);
		group.AddChild(unLabelButton);
		group.AddChild(hideButton);
		group.AddChild(label);

		_associatedWays[radioButtonGroup] = ways;
		_toggles.AddChild(group);

		y += ToggleCheckBoxHeight;
	}

	public override void Initialize() {
		_ui.AddChild(_expandNorth);
		_ui.AddChild(_contractNorth);
		_ui.AddChild(_expandWest);
		_ui.AddChild(_contractWest);
		_ui.AddChild(_contractEast);
		_ui.AddChild(_expandEast);
		_ui.AddChild(_contractSouth);
		_ui.AddChild(_expandSouth);

		_expandNorth.OnClick += () => {
			_queryResult.BBox.YMax += BBoxIncrement;
			RefreshBBoxButtons();
		};
		_contractNorth.OnClick += () => {
			_queryResult.BBox.YMax -= BBoxIncrement;
			RefreshBBoxButtons();
		};
		_expandWest.OnClick += () => {
			_queryResult.BBox.XMin -= BBoxIncrement;
			RefreshBBoxButtons();
		};
		_contractWest.OnClick += () => {
			_queryResult.BBox.XMin += BBoxIncrement;
			RefreshBBoxButtons();
		};
		_contractEast.OnClick += () => {
			_queryResult.BBox.XMax -= BBoxIncrement;
			RefreshBBoxButtons();
		};
		_expandEast.OnClick += () => {
			_queryResult.BBox.XMax += BBoxIncrement;
			RefreshBBoxButtons();
		};
		_contractSouth.OnClick += () => {
			_queryResult.BBox.YMin += BBoxIncrement;
			RefreshBBoxButtons();
		};
		_expandSouth.OnClick += () => {
			_queryResult.BBox.YMin -= BBoxIncrement;
			RefreshBBoxButtons();
		};

		_ui.AddChild(_toggles);
		_ui.AddChild(_showAll);
		_ui.AddChild(_showAreas);
		_ui.AddChild(_showPaths);
		_ui.AddChild(_unLabelAll);
		_ui.AddChild(_unLabelAreas);
		_ui.AddChild(_unLabelPaths);
		_ui.AddChild(_hideAll);
		_ui.AddChild(_hideAreas);
		_ui.AddChild(_hidePaths);

		_refreshBBox.OnClick += RepopulateBBox;
		_newMapObjName.OnSubmit += AddNewMapObject;
		_addNewMapObj.OnClick += () => { AddNewMapObject(_newMapObjName.Builder); };

		_ui.AddChild(_refreshBBox);
		_ui.AddChild(_newMapObjName);
		_ui.AddChild(_addNewMapObj);

		_showAll.OnClick += ShowAll;
		_showAreas.OnClick += ShowAreas;
		_showPaths.OnClick += ShowPaths;
		_unLabelAll.OnClick += UnLabelAll;
		_unLabelAreas.OnClick += UnLabelAreas;
		_unLabelPaths.OnClick += UnLabelPaths;
		_hideAll.OnClick += HideAll;
		_hideAreas.OnClick += HideAreas;
		_hidePaths.OnClick += HidePaths;

		_toggles.InnerSizePadding = 16;
		var y = 0f;
		foreach (var name in OsmWay.GetTagsByKey("name"))
			AddWayItem(OsmWay.GetWaysByTag("name", name).ToList(), name, ref y);

		AddWayItem(
			OsmWay.GetWaysWithoutTag("name")
				.Where(way => way.IsPolygon && way.Tags.TryGetValue("building", out var building) && building != "no")
				.ToList(),
			"Unnamed Buildings",
			ref y
		);
		AddWayItem(
			OsmWay.GetWaysWithoutTag("name")
				.Where(way => way.IsPolygon && way.Tags.ContainsKey("water"))
				.ToList(),
			"Unnamed Bodies of Water",
			ref y
		);
		AddWayItem(
			OsmWay.GetWaysWithoutTag("name")
				.Where(way => way.IsPolygon && (!way.Tags.TryGetValue("building", out var building) || building == "no") && !way.Tags.ContainsKey("water"))
				.ToList(),
			"Unnamed Land Areas",
			ref y
		);
		AddWayItem(
			OsmWay.GetWaysWithoutTag("name")
				.Where(way => way.IsHighway && way.Tags.TryGetValue("highway", out var highway) && highway != "footway")
				.ToList(),
			"Unnamed Roads",
			ref y
		);
		AddWayItem(
			OsmWay.GetWaysWithoutTag("name")
				.Where(way => way.IsHighway && way.Tags.TryGetValue("highway", out var highway) && highway == "footway")
				.ToList(),
			"Unnamed Footpaths",
			ref y
		);
		AddWayItem(
			OsmWay.GetWaysWithoutTag("name")
				.Where(way => way.IsHighway && !way.Tags.ContainsKey("highway"))
				.ToList(),
			"Unnamed Misc. Paths",
			ref y
		);

		_ui.AddChild(_exportMap);
		_ui.AddChild(_closeMap);

		_exportMap.OnClick += ExportMap;
		_closeMap.OnClick += CloseMap;
	}

	public override void HandleInput(in InputEvent input) {
		if (!FmsApp.Instance.HideUi)
			_ui.HandleInput(in input);
	}

	private const float ArrowWidth = 128;
	private const float ArrowMargin = 80;
	private const float ArrowLength = 160;

	private readonly SKPaint _bgPaint = CreatePaint();
	private readonly SKPaint _areaPaint = CreatePaint();
	private readonly SKPaint _waterPaint = CreatePaint();
	private readonly SKPaint _roadPaint = CreatePaint();
	private readonly SKPaint _footwayPaint = CreatePaint();
	private readonly SKPaint _linePaint = CreatePaint();
	private readonly SKPaint _textPaint = CreatePaint();
	private readonly SKPaint _arrowPaint = CreatePaint();
	private readonly SKRect _screenRect = new(0, 0, 1200, 900);
	private readonly SKRect _noUiScreenRect = new(0, 0, 1600, 900);
	private SKRect ScreenRect => FmsApp.Instance.HideUi ? _noUiScreenRect : _screenRect;

	public override void Render(SKCanvas canvas) {
		_bgPaint.Color = SKColors.White;

		var textScaling = GetTextScaling(_queryResult, ScreenRect);

		_linePaint.Style = SKPaintStyle.Stroke;
		_linePaint.StrokeWidth = (float)(1 * textScaling);
		_linePaint.Color = SKColors.Gray.WithAlpha(51);
		_linePaint.BlendMode = SKBlendMode.SrcOver;

		_waterPaint.Style = SKPaintStyle.Fill;
		_waterPaint.Color = SKColors.RoyalBlue.WithAlpha(153);
		_waterPaint.BlendMode = SKBlendMode.SrcOver;

		_areaPaint.Style = SKPaintStyle.Fill;
		_areaPaint.Color = SKColors.SlateGray.WithAlpha(102);
		_areaPaint.BlendMode = SKBlendMode.SrcOver;

		_roadPaint.Style = SKPaintStyle.Stroke;
		_roadPaint.StrokeWidth = (float)(3 * textScaling);
		_roadPaint.Color = SKColors.SteelBlue.WithAlpha(153);
		_roadPaint.BlendMode = SKBlendMode.SrcOver;

		_footwayPaint.Style = SKPaintStyle.Stroke;
		_footwayPaint.StrokeWidth = (float)(2 * textScaling);
		_footwayPaint.Color = SKColors.DarkOrange.WithAlpha(204);
		_footwayPaint.BlendMode = SKBlendMode.SrcOver;

		_textPaint.Color = SKColors.Black;
		_textPaint.SelectFont(new FontSpec { Bold = true });
		_textPaint.TextSize = (float)(textScaling * ImageTextSize);

		_arrowPaint.Style = SKPaintStyle.Fill;
		_arrowPaint.Color = SKColors.LightSlateGray.WithAlpha(204);
		_arrowPaint.BlendMode = SKBlendMode.SrcOver;

		DrawMap(canvas, _queryResult, ScreenRect, _bgPaint, _areaPaint, _waterPaint, _roadPaint, _footwayPaint, _linePaint, _textPaint, _arrowPaint, (float)(ArrowMargin * textScaling), (float)(ArrowWidth * textScaling), (float)(ArrowLength * textScaling));

		_ui.Tick();
		if (!FmsApp.Instance.HideUi)
			_ui.Render(canvas);
	}

	private static void DrawMapElement(SKCanvas canvas, OsmWay element, bool close, OsmQueryResult result, in SKRect screenBBox, SKPaint paint) {
		using var skPath = new SKPath();
		var isMoveTo = true;
		foreach (var node in element.Nodes) {
			if (node.Location.IsNowhere)
				continue;

			if (isMoveTo) {
				isMoveTo = false;
				skPath.MoveTo(node.Location.Draw(in result.BBox, in screenBBox));
			}
			else {
				skPath.LineTo(node.Location.Draw(in result.BBox, in screenBBox));
			}
		}

		if (close)
			skPath.Close();

		canvas.DrawPath(skPath, paint);
	}

	private static void DrawMap(SKCanvas canvas, OsmQueryResult result, in SKRect screenRect, SKPaint bgPaint, SKPaint areaPaint, SKPaint waterPaint, SKPaint roadPaint, SKPaint footwayPaint, SKPaint linePaint, SKPaint textPaint, SKPaint arrowPaint, float arrowMargin, float arrowWidth, float arrowLength) {
		var screenBBox = result.BBox.Draw(in screenRect);

		canvas.Save();
		canvas.ClipRect(screenBBox);
		canvas.DrawPaint(bgPaint);

		var polygons = result.Ways.Where(way => way is { Visibility: not VisibilityState.Hide, IsPolygon: true }).ToList();
		var highways = result.Ways.Where(way => way is { Visibility: not VisibilityState.Hide, IsHighway: true }).ToList();

		// Misc. ways
		foreach (var highway in highways) {
			if (highway.Tags.ContainsKey("highway"))
				continue;

			DrawMapElement(canvas, highway, false, result, in screenBBox, linePaint);
		}

		// Bodies of water
		foreach (var polygon in polygons) {
			if (!polygon.Tags.ContainsKey("water"))
				continue;

			DrawMapElement(canvas, polygon, true, result, in screenBBox, waterPaint);
		}

		// Areas and Buildings
		foreach (var polygon in polygons) {
			if (polygon.Tags.ContainsKey("water"))
				continue;

			DrawMapElement(canvas, polygon, true, result, in screenBBox, areaPaint);
		}

		// Roads
		foreach (var highway in highways) {
			if (!highway.Tags.TryGetValue("highway", out var value) || value == "footway")
				continue;

			DrawMapElement(canvas, highway, false, result, in screenBBox, roadPaint);
		}

		// Footways
		foreach (var highway in highways) {
			if (!highway.Tags.TryGetValue("highway", out var value) || value != "footway")
				continue;

			DrawMapElement(canvas, highway, false, result, in screenBBox, footwayPaint);
		}

		// Text
		foreach (var way in result.Ways) {
			if (way.Visibility != VisibilityState.Show || !way.Tags.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
				continue;

			var nameMutable = MutableString.RentedCopyOf(name.AsSpan());
			var center = way.Center;
			if (!ArrowRenderer.GetArrowHeadLocation(in center, in result.BBox, in screenBBox, arrowMargin, out var drawPoint, out var arrowHead)) {
				canvas.DrawAnchoredText(
					nameMutable,
					drawPoint,
					HorizontalAnchor.Middle,
					VerticalAnchor.Middle,
					new TextRenderStyle(),
					textPaint,
					out _
				);

				continue;
			}

			using var arrowPath = ArrowRenderer.DrawArrow(in arrowHead, in drawPoint, arrowWidth, arrowLength, out var textLocation);
			canvas.DrawPath(arrowPath, arrowPaint);
			canvas.DrawAnchoredText(
				nameMutable,
				textLocation,
				HorizontalAnchor.Middle,
				VerticalAnchor.Middle,
				new TextRenderStyle(),
				textPaint,
				out _
			);
		}

		canvas.Restore();
	}

	private ApplicationMode? _next;
	public override ApplicationMode NextMode => _next ?? this;
}
