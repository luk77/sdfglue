using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SingleDocAppCore.Model.BaseTypes;

namespace SdfGlueCore.AbstractRenderer
{
    public interface IRenderingSystem : IDisposable
    {
        // Recreates the passes of model.RenderingSysData
        void Reinitialize(DataModel model, ShaderCodeGenerator codeGenerator, IntCoords previewResolution);
        void ReinitializeShader(DataModel model, ShaderCodeGenerator codeGenerator);
        void ReinitializePreviewFrameBuffer(IntCoords previewResolution);
        void RenderFrame(DataModel model, FrameRenderParams frame, int appViewportWidth, int appViewportHeight);
        // Clears the whole application window to black (e.g. bars around a preview drawn directly into the window)
        void ClearWindowToBlack();
   }
}
