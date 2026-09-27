using SingleDocAppCore.UndoSystem;
using SingleDocAppFramework.Input;
using SingleDocAppFramework.Layouts;
using SingleDocAppFramework.Platform;
using SingleDocAppFramework.Ui;

namespace SingleDocAppFramework
{
    // Default implementation of IUiExecutorFramework.
    // Input, layouts, undo/redo, exit, scaling and clipboard are handled here.
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
