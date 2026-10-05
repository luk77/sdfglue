using SingleDocAppCore.Input;
using SingleDocAppCore.Settings;
using SingleDocAppCore.UndoSystem;
using SingleDocAppFramework.Input;
using SingleDocAppFramework.Layouts;
using SingleDocAppFramework.Platform;
using SingleDocAppFramework.Ui;

namespace SingleDocAppFramework
{
    // Default implementation of IUiExecutorFramework.
    // Input, layouts, user settings, undo/redo, exit, scaling and clipboard are handled here.
    // Document commands are empty - the application overrides them in its own executor:
    //     class MyAppExecutor : UiExecutorFrameworkBase, IMyAppExecutor   (IMyAppExecutor : IUiExecutorFramework)
    public class UiExecutorFrameworkBase : IUiExecutorFramework
    {
        protected   SdAppWindow             window_;

        public UiExecutorFrameworkBase(SdAppWindow window)
        {
            window_ = window;
        }

        // document (to be implemented by the application)
        public virtual void OnNewDocument       ()                  { }
        public virtual void OnOpenDocument      ()                  { }
        public virtual void OnOpenDocument      (string filePath)   { }
        public virtual void OnSaveDocument      ()                  { }
        public virtual void OnSaveDocumentAs    ()                  { }
        public virtual bool CanSaveDocument     ()                  { return false; }

        // Unsaved changes are tracked by UndoManager: the application calls UndoManager.Instance.MarkSavePoint()
        // after a successful save and ClearAll() after new/open.
        public virtual bool IsDocumentModified()
        {
            return UndoManager.Instance.IsModified;
        }

        // Asks whether to save unsaved changes before they are discarded (new/open document, exit).
        // Returns false if the operation should be cancelled (Cancel, or the document was not saved).
        public virtual bool ConfirmDiscardChanges()
        {
            if (!IsDocumentModified())
                return true;

            DialogAnswer answer = window_.Platform.AskYesNoCancel(window_.AppSettings.AppName, "The document has unsaved changes.\n\nSave them?");
            if (answer == DialogAnswer.Cancel)
                return false;
            if (answer == DialogAnswer.No)
                return true;

            if (CanSaveDocument())
                OnSaveDocument();
            else
                OnSaveDocumentAs();

            return !IsDocumentModified();
        }

        // undo / redo
        public virtual void OnUndo()
        {
            UndoManager.Instance.DoUndo();
        }

        public virtual void OnRedo()
        {
            UndoManager.Instance.DoRedo();
        }

        // layouts
        public virtual void OnLoadLayout(string layoutFilePath)
        {
            window_.OnLoadLayout(layoutFilePath);
        }

        public virtual void OnSaveCurrentLayout(WindowsVisibilityCollection windowsVisibility)
        {
            window_.OnSaveCurrentLayout(windowsVisibility);
        }

        // user settings
        public UserSettingsBase GetUserSettings()
        {
            return window_.UserSettings;
        }

        public virtual void OnSaveUserSettings()
        {
            window_.SaveUserSettings();
        }

        public virtual void OnRestoreDefaultUserSettings()
        {
            window_.RestoreDefaultUserSettings();
        }

        public InputSystem? GetInputSystem()
        {
            return window_.Inputs;
        }

        // application
        public virtual void OnExitApp()
        {
            window_.OnExitApp();
        }

        public float GetWindowsScaling()
        {
            return window_.GetWindowsScaling();
        }

        public void CopyTextToClipboard(string text)
        {
            ClipboardUtils.SetText(text);
        }

        // IUiInput (delegated to the window)
        public bool     IsKeyDown           (UiKey key)     { return window_.IsKeyDown(key);        }
        public bool     IsKeyPressed        (UiKey key)     { return window_.IsKeyPressed(key);     }
        public bool     IsDownAnyCtrl       ()              { return window_.IsDownAnyCtrl();       }
        public bool     IsDownAnyShift      ()              { return window_.IsDownAnyShift();      }
        public bool     IsDownAnyAlt        ()              { return window_.IsDownAnyAlt();        }
        public float    GetWheelPrecise     ()              { return window_.GetWheelPrecise();     }
        public bool     IsRmbDown           ()              { return window_.IsRmbDown();           }
        public bool     IsLmbDown           ()              { return window_.IsLmbDown();           }
        public int      GetMouseStateX      ()              { return window_.GetMouseStateX();      }
        public int      GetMouseStateY      ()              { return window_.GetMouseStateY();      }
    }
}
