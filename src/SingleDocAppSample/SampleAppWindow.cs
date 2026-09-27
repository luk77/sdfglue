using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using SingleDocAppFramework;
using SingleDocAppFramework.Platform;
using SingleDocAppFramework.Ui;

namespace SingleDocAppSample
{
    // Main window of the sample application.
    // Everything generic (ImGui, layouts, menus, shortcuts, undo) comes from SdAppWindow,
    // the application provides only: settings, document, executor, UI manager and 3D content.
    public class SampleAppWindow : SdAppWindow
    {
        public  TextDocument            Document                = new TextDocument();

        public SampleAppWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, IPlatformServices platform)
            : base(gameWindowSettings, nativeWindowSettings, CreateAppSettings(), platform)
        {
        }

        private static SdAppSettings CreateAppSettings()
        {
            SdAppSettings settings = new SdAppSettings();
            settings.AppName            = "SingleDocApp Sample";
            settings.DocumentFileFilter = new FileFilter("Text files", "*.txt");
            return settings;
        }

        protected override UiExecutorFrameworkBase CreateExecutor()
        {
            return new SampleExecutor(this);
        }

        protected override UiManagerBase CreateUiManager(UiExecutorFrameworkBase executor)
        {
            return new UiManagerSample((ISampleExecutor)executor, ClientSize.X, ClientSize.Y);
        }

        protected override string? GetDocumentDisplayName()
        {
            return String.IsNullOrEmpty(Document.FilePath) ? "<Unnamed>" : Document.FilePath;
        }

        // Background behind the ImGui windows (an application could render a 2D/3D scene here)
        protected override void OnRender3d()
        {
            GL.ClearColor(0.12f, 0.12f, 0.14f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
}
