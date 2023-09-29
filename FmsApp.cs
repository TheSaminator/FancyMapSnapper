using System.Collections.Concurrent;
using System.Text;
using FancyMapSnapper.AppMode;
using FancyMapSnapper.Ui;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace FancyMapSnapper;

public sealed class FmsApp : GameWindow {
	private FmsApp(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) {
		CenterWindow();

		Instance = this;
	}

	private GRGlInterface _grgInterface = null!;
	private GRContext _grContext = null!;
	private SKSurface _surface = null!;
	private SKCanvas _canvas = null!;
	private GRBackendRenderTarget _renderTarget = null!;

	private readonly ConcurrentQueue<Action> _postedActions = new();
	private readonly ConcurrentQueue<Action> _queuedActions = new();

	public void PostAction(Action action) {
		_postedActions.Enqueue(action);
	}

	private ApplicationMode _mode = new Trampoline();

	public bool HideUi { get; private set; }

	protected override void OnLoad() {
		GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

		_grgInterface = GRGlInterface.Create();
		_grContext = GRContext.CreateGl(_grgInterface);
		_renderTarget = new GRBackendRenderTarget(ClientSize.X, ClientSize.Y, GL.GetInteger(GetPName.Samples), 8, new GRGlFramebufferInfo(0, (uint)SizedInternalFormat.Rgba8));

		_surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
		_canvas = _surface.Canvas;
	}

	protected override void OnUpdateFrame(FrameEventArgs args) {
		while (_postedActions.TryDequeue(out var postedAction))
			_queuedActions.Enqueue(postedAction);

		while (_queuedActions.TryDequeue(out var queuedAction))
			queuedAction();

		var nextMode = _mode.NextMode;
		if (nextMode == _mode) return;

		_mode = _mode.NextMode;
		_mode.Initialize();
	}

	protected override void OnKeyDown(KeyboardKeyEventArgs e) {
		if (e.Key == Keys.F11) {
			HideUi = true;
			return;
		}

		_mode.HandleInput(new InputEvent(new InputEventKeyboardKey(e.Key, InputEventAction.Press)));
	}

	protected override void OnKeyUp(KeyboardKeyEventArgs e) {
		if (e.Key == Keys.F11) {
			HideUi = false;
			return;
		}

		_mode.HandleInput(new InputEvent(new InputEventKeyboardKey(e.Key, InputEventAction.Release)));
	}

	protected override void OnTextInput(TextInputEventArgs e) {
		if (Rune.TryCreate(e.Unicode, out var rune))
			_mode.HandleInput(new InputEvent(new InputEventTextWritten(rune)));
	}

	protected override void OnMouseDown(MouseButtonEventArgs e) {
		_mode.HandleInput(new InputEvent(new InputEventMouseButton(e.Button, InputEventAction.Press)));
	}

	protected override void OnMouseUp(MouseButtonEventArgs e) {
		_mode.HandleInput(new InputEvent(new InputEventMouseButton(e.Button, InputEventAction.Release)));
	}

	protected override void OnMouseMove(MouseMoveEventArgs e) {
		_mode.HandleInput(new InputEvent(new InputEventCursorMoved(e.Position - e.Delta, e.Position)));
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e) {
		if (e.OffsetY != 0)
			_mode.HandleInput(new InputEvent(new InputEventScrollWheel(e.OffsetY)));
	}

	protected override void OnRenderFrame(FrameEventArgs args) {
		GL.Enable(EnableCap.Blend);
		GL.Enable(EnableCap.CullFace);
		GL.Enable(EnableCap.DepthTest);
		GL.Enable(EnableCap.TextureCubeMapSeamless);
		GL.CullFace(CullFaceMode.Back);
		GL.DepthMask(true);
		GL.ClearColor(0, 0, 0, 1);
		GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

		_grContext.ResetContext();
		_mode.Render(_canvas);
		_canvas.Flush();
		_grContext.Submit();

		SwapBuffers();
	}

	protected override void OnUnload() {
		_surface.Dispose();
		_renderTarget.Dispose();
		_grContext.Dispose();
		_grgInterface.Dispose();
	}

	public static FmsApp Instance { get; private set; } = null!;

	public static void Main(string[] args) {
		using var app = new FmsApp(
			new GameWindowSettings(),
			new NativeWindowSettings {
				Title = "Fancy Map Snapper",
				Vsync = VSyncMode.Adaptive,
				APIVersion = new Version(3, 2),
				Size = new Vector2i(1600, 900),
				WindowBorder = WindowBorder.Fixed
			}
		);
		app.Run();
	}
}
