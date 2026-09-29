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
            return appWindow_.Document;
        }

        // document commands required by the framework

        public override void OnNewDocument()
        {
            if (!ConfirmDiscardChanges())
                return;

            SetDocument(new TextDocument());
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

            SetDocument(document);
        }

        public override void OnSaveDocument()
        {
            if (!CanSaveDocument())
                return;

            GetDocument().Save(GetDocument().FilePath!);
            UndoManager.Instance.MarkSavePoint();
            window_.RefreshWindowTitle();
        }

        public override void OnSaveDocumentAs()
        {
            string? filePath = window_.Platform.SaveFileDialog(window_.AppSettings.DocumentFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            GetDocument().Save(filePath);
            UndoManager.Instance.MarkSavePoint();
            window_.RefreshWindowTitle();
        }

        public override bool CanSaveDocument()
        {
            return !String.IsNullOrEmpty(GetDocument().FilePath);
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

        private void SetDocument(TextDocument document)
        {
            appWindow_.Document = document;

            // undo actions refer to the objects of the previous document
            UndoManager.Instance.ClearAll();

            window_.RefreshWindowTitle();
        }
    }
}
