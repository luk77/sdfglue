//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.CodeFragments
{
    // Function definitions loaded from the Functions/ folder. They do not depend on the project,
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

        public void Reload()
        {
            Renderers               .Reload("Functions/Renderers/");
            Backdrops               .Reload("Functions/Backdrops/");
            CameraControllers       .Reload("Functions/CameraOperators/");
            Sdf                     .Reload("Functions/Sdf/");
            MixOps                  .Reload("Functions/MixOperators/");
            PositionOps             .Reload("Functions/PositionOperators/");
            DistanceOps             .Reload("Functions/DistanceOperators/");
        }
    }
}
