using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppFramework;
using SingleDocAppFramework.Ui;

namespace SingleDocAppSample
{
    // Application-specific UI actions (the generic ones come from IUiExecutorFramework)
    public interface ISampleExecutor : IUiExecutorFramework
    {
        TextDocument    GetDocument     ();
        void            OnToUpperCase   ();
        void            OnToLowerCase   ();
    }

    public class SampleExecutor : UiExecutorFrameworkBase, ISampleExecutor
    {
        private SampleAppWindow         appWindow_;

        public SampleExecutor(SampleAppWindow window)
            : base(window)
        {
            appWindow_ = window;
        }

        public TextDocument GetDocument()
        {
            return appWindow_.AppContext.Document;
        }

        // document commands required by the framework

        public override void OnNewDocument()
        {
            if (!ConfirmDiscardChanges())
                return;

            appWindow_.AppContext.SetDocument(new TextDocument(), null);
        }

        public override void OnOpenDocument()
        {
            if (!ConfirmDiscardChanges())
                return;

            string? filePath = window_.Platform.OpenFileDialog(window_.AppSettings.DocumentFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            OnOpenDocument(filePath);
        }

        public override void OnOpenDocument(string filePath)
        {
            TextDocument document = new TextDocument();
            document.Load(filePath);

            appWindow_.AppContext.SetDocument(document, filePath);
        }

        public override void OnSaveDocument()
        {
            if (!CanSaveDocument())
                return;

            GetDocument().Save(appWindow_.AppContext.DocumentFilePath!);
            UndoManager.Instance.MarkSavePoint();
            window_.RefreshWindowTitle();
        }

        public override void OnSaveDocumentAs()
        {
            string? filePath = window_.Platform.SaveFileDialog(window_.AppSettings.DocumentFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            GetDocument().Save(filePath);
            appWindow_.AppContext.DocumentFilePath = filePath;
            UndoManager.Instance.MarkSavePoint();
            window_.RefreshWindowTitle();
        }

        public override bool CanSaveDocument()
        {
            return !String.IsNullOrEmpty(appWindow_.AppContext.DocumentFilePath);
        }

        // application commands (undoable)

        public void OnToUpperCase()
        {
            ChangeText(GetDocument().GetText().ToUpperInvariant());
        }

        public void OnToLowerCase()
        {
            ChangeText(GetDocument().GetText().ToLowerInvariant());
        }

        private void ChangeText(string newText)
        {
            TextDocument document = GetDocument();
            if (newText == document.GetText())
                return;

            document.Text.Val = newText;

            // ActionString stores PrevVal -> Val, so the change can be undone/redone
            UndoManager.Instance.SaveAction(new ActionString(document.Text));
        }
    }
}
