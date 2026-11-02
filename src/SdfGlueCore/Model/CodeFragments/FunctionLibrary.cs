//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.CodeFragments
{
    // Function definitions loaded from the Functions/ folder of the workspace (SdfGlueWorkspace). They do not depend on the project,
    // so one library (owned by the application context) is shared by all models - current and temporary ones.
    // After Reload() the models refresh their references (DataModel.RefreshDefinitions()).
    public class FunctionLibrary
    {
        public FunctionDefinitionsSet   Renderers               = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   Backdrops               = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   CameraControllers       = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   Sdf                     = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   MixOps                  = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   PositionOps             = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   DistanceOps             = new FunctionDefinitionsSet();

        // Incremented by every Reload() (data derived from the definitions can be cached, e.g. code highlighting)
        public int                      Version                 { get; private set; } = 0;

        // All sets (in a fixed order)
        public IEnumerable<FunctionDefinitionsSet> GetAllSets()
        {
            yield return Renderers;
            yield return Backdrops;
            yield return CameraControllers;
            yield return Sdf;
            yield return MixOps;
            yield return PositionOps;
            yield return DistanceOps;
        }

        public void Reload()
        {
            Version++;
            Renderers               .Reload(SdfGlueWorkspace.GetPath("Functions/Renderers/"));
            Backdrops               .Reload(SdfGlueWorkspace.GetPath("Functions/Backdrops/"));
            CameraControllers       .Reload(SdfGlueWorkspace.GetPath("Functions/CameraOperators/"));
            Sdf                     .Reload(SdfGlueWorkspace.GetPath("Functions/Sdf/"));
            MixOps                  .Reload(SdfGlueWorkspace.GetPath("Functions/MixOperators/"));
            PositionOps             .Reload(SdfGlueWorkspace.GetPath("Functions/PositionOperators/"));
            DistanceOps             .Reload(SdfGlueWorkspace.GetPath("Functions/DistanceOperators/"));
        }
    }
}
