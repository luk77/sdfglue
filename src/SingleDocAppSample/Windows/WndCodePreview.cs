using ImGuiNET;
using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.CodeView;

namespace SingleDocAppSample.Windows
{
    // The text of the document in the framework code view (syntax highlighting, line numbers)
    public class WndCodePreview : UiWindowBase
    {
        public override string Title => "Code Preview";

        private ISampleExecutor SampleExecutor { get { return (ISampleExecutor)Executor; } }

        private readonly CodeLanguage[]     languages_      = { CodeLanguages.CLike(), CodeLanguages.Glsl(), CodeLanguages.Hlsl() };
        private readonly string[]           languageNames_;
        private int                         languageIndex_  = 0;

        private readonly CodeViewDocument   document_       = new CodeViewDocument();
        private readonly CodeViewOptions    options_        = new CodeViewOptions();

        public WndCodePreview()
        {
            languageNames_ = languages_.Select(l => l.Name).ToArray();
        }

        public override void Build()
        {
            BuildWindow(delegate()
            {
                ImGui.SetNextItemWidth(150.0f);
                ImGui.Combo("Language", ref languageIndex_, languageNames_, languageNames_.Length);
                ImGui.SameLine();
                ImGui.Checkbox("Line numbers", ref options_.ShowLineNumbers);

                // tokenized only when the text or the language changes
                document_.SetText(SampleExecutor.GetDocument().GetText(), languages_[languageIndex_]);
                UiCodeView.Build("##code", document_, options_);
            });
        }
    }
}
