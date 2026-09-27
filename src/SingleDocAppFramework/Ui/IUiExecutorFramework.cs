using SingleDocAppCore.Settings;
using SingleDocAppFramework.Input;
using SingleDocAppFramework.Layouts;

namespace SingleDocAppFramework.Ui
{
    // Actions requested by the UI that every single-document application supports.
    // The framework knows nothing about the document type - the application decides
    // what "new", "open" and "save" mean.
    public interface IUiExecutorFramework : IUiInput
    {
        // document
        void    OnNewDocument       ();
        void    OnOpenDocument      ();                 // shows the "open file" dialog
        void    OnOpenDocument      (string filePath);
        void    OnSaveDocument      ();
        void    OnSaveDocumentAs    ();
        bool    CanSaveDocument     ();                 // false if the document has no file path yet

        // undo / redo
        void    OnUndo              ();
        void    OnRedo              ();

        // layouts
        void    OnLoadLayout        (string layoutFilePath);
        void    OnSaveCurrentLayout (WindowsVisibilityCollection windowsVisibility);

        // user settings
        UserSettingsBase GetUserSettings            ();
        void    OnSaveUserSettings                  ();
        void    OnRestoreDefaultUserSettings        ();     // in memory only

        // application
        void    OnExitApp           ();
        float   GetWindowsScaling   ();
        void    CopyTextToClipboard (string text);
    }
}
