namespace SingleDocAppFramework.Input
{
    // Keyboard and mouse state, independent of the windowing library (OpenTK)
    public interface IUiInput
    {
        bool    IsKeyDown           (UiKey key);
        bool    IsKeyPressed        (UiKey key);
        bool    IsDownAnyCtrl       ();
        bool    IsDownAnyShift      ();
        bool    IsDownAnyAlt        ();
        float   GetWheelPrecise     ();
        bool    IsRmbDown           ();
        bool    IsLmbDown           ();
        int     GetMouseStateX      ();
        int     GetMouseStateY      ();
    }
}
