using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes;

namespace SdfGlueCore.AbstractRenderer
{
    public interface IRenderingSystem : IDisposable
    {
        void Reinitialize(RenderingData renderingData, ShaderCodeGenerator codeGenerator, IntCoords previewResolution);
        void ReinitializeShader(ShaderCodeGenerator codeGenerator);
        void ReinitializePreviewFrameBuffer(IntCoords previewResolution);
        void RenderFrame(DataModel model, int appViewportWidth, int appViewportHeight);
   }
}
