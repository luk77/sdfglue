//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model;
using SingleDocAppCore.UndoSystem;

namespace SingleDocAppCore.Controller
{
    // Optional base class of the application context: owns the current document and its file path.
    // The document is replaced only by SetDocument() (new/open document), so the derived context can
    // refresh everything that depends on it in OnDocumentChanged() (rendering, window title etc.).
    // State which is not saved in the document (playback, selection...) belongs to the derived context.
    public class SdAppContextBase<TDocument> where TDocument : class, IAbstractDocument
    {
        public  TDocument               Document                { get; private set; }

        // Path of the file the document was loaded from / saved to, null for a new document
        public  string?                 DocumentFilePath        { get; set; }

        public SdAppContextBase(TDocument document)
        {
            Document = document;
        }

        public void SetDocument(TDocument document, string? filePath)
        {
            Document            = document;
            DocumentFilePath    = filePath;

            // undo actions refer to the objects of the previous document
            UndoManager.Instance.ClearAll();

            OnDocumentChanged();
        }

        // Called after the document has been replaced
        protected virtual void OnDocumentChanged()
        {
        }
    }
}
