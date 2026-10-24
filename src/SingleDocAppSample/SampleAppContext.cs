using SingleDocAppCore.Controller;

namespace SingleDocAppSample
{
    // Application state: the current document (and its file path) owned by SdAppContextBase
    public class SampleAppContext : SdAppContextBase<TextDocument>
    {
        private SampleAppWindow         window_;

        public SampleAppContext(SampleAppWindow window)
            : base(new TextDocument())
        {
            window_ = window;
        }

        protected override void OnDocumentChanged()
        {
            window_.RefreshWindowTitle();
        }
    }
}
