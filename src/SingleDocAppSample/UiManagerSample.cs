using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.Menu;
using SingleDocAppFramework.Ui.Windows;
using SingleDocAppSample.Windows;

namespace SingleDocAppSample
{
    public class UiManagerSample : UiManagerBase
    {
        public UiManagerSample(ISampleExecutor executor, int mainWindowSizeX, int mainWindowSizeY)
            : base(executor)
        {
            // Window titles are the keys in layout files - do not change them once layouts are saved
            RegisterWindow(new WndEditor());
            RegisterWindow(new WndStats(), "Info");
            RegisterWindow(new WndLog(), "Info", false);

            SetMainWindowClientSize(mainWindowSizeX, mainWindowSizeY);
        }

        public ISampleExecutor SampleExecutor { get { return (ISampleExecutor)Executor; } }

        protected override MainMenuBase CreateMainMenu()
        {
            return new MainMenuSample(this);
        }
    }
}
