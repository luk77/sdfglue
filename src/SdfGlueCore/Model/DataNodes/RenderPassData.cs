//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.AbstractRenderer;
using SdfGlueCore.Controller;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;
using SdfGlueCore.Utils;
using System.Drawing;
using System.Text;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes
{
    public class RenderPassData : SerializableNode
    {
        public  IRenderingPass?         RenderingPass                   = null;

        public  CodeGenResult           LastGenCode                     = new CodeGenResult();
        public  string?                 LastGenCodeUnity                = null;
        public  string                  LastErrors                      = "";

        public  bool                    IsPrimaryPass                   = false;
        public  bool                    ForceClear                      = false;
        public  ExBool                  Enabled                         = new ExBool            (true);
        public  ExBool                  IsFixed                         = new ExBool            (false);
        public  ExBool                  ClearOnEveryFrame               = new ExBool            (true);
        public  ExVector3               ClearColor                      = new ExVector3         ();//(new Vector3(0.0f));
        public  ExFloat                 ClearOpacity                    = new ExFloatWithSignal (1.0f);
        public  ExBool                  AutoResetFrameCounter           = new ExBool            (true);
        public  RenderPassEntity        RendererFunc;
        public  BackdropEntity          BackdropFunc;
        //public  CameraControllerEntity  CameraCtrlFunc;
        public  OperatorsCollection     CameraOperators;                // operatory nakładane na parametry kamery

        public  ExBool                  UseTextureFiltering             = new ExBool    (true);

        // Diagnostics
        //public  Int64                   LastRenderTime                  = 0;
        //public  Int64                   RenderTimeAccumValue            = 0;
        //public  Int64                   RenderTimeAccumCounter          = 0;
        //public  double                  AverageRenderTimeMs             = 0.0;

        // Parameterless constructor (for copy/paste serialization)
        public RenderPassData() : this(0, "", null, false) { }

        public RenderPassData(int id, string name, FunctionDefinition? definition, bool primaryPass) : base(id, name)
        {
            IsPrimaryPass = primaryPass;

            RendererFunc = new RenderPassEntity();
            if (definition == null)
            {
                RendererFunc.DefinitionName.Val      = IsPrimaryPass ? "Default" : "SecPassDefault";
            }
            else
            {
                RendererFunc.DefinitionName.Val      = definition.FunctionName;
            }

            BackdropFunc = new BackdropEntity();
            BackdropFunc.DefinitionName.Val = "backdropDefault";

            //CameraCtrlFunc = new CameraControllerEntity();
            //CameraCtrlFunc.DefinitionName.Val = "cameraSimpleOrbiter";

            CameraOperators = new OperatorsCollection("cameraOp");

            if (IsPrimaryPass)
            {
                OperatorEntity defaultCamera = CameraOperators.CreateNewEntity();
                defaultCamera.DefinitionName.Val = "cameraOpDefault";
                CameraOperators.Operators.Insert(0, defaultCamera);
            }

            //RendererFunc.RefreshDefinitionReference(model.Renderers);
        }

        public int GetTextureId()
        {
            if (RenderingPass == null)
                return 0;

            return RenderingPass.GetTextureId();
        }

        public Bitmap? GetFrameAsBitmap(IntCoords textureSize)
        {
            if (RenderingPass == null)
                return null;

            return RenderingPass.GetFrameAsBitmap(textureSize);
        }

        public void RefreshDefinitionReference(FunctionDefinitionsSet renderers, FunctionDefinitionsSet backdrops, FunctionDefinitionsSet cameraControllers)
        {
            RendererFunc    .RefreshDefinitionReference(renderers);
            BackdropFunc    .RefreshDefinitionReference(backdrops);
            //CameraCtrlFunc  .RefreshDefinitionReference(cameraControllers);
            CameraOperators .RefreshDefinitionReference(cameraControllers);
        }

//        public void SetLastRenderTime(Int64 elapsedMs)
//        {
//            LastRenderTime          = 1000 * elapsedMs;
//            RenderTimeAccumValue    += 1000 * elapsedMs;
//            RenderTimeAccumCounter  ++;
//
//            if (RenderTimeAccumCounter > 60)
//            {
//                AverageRenderTimeMs = RenderTimeAccumValue / ((double)60);
//
//                RenderTimeAccumValue = 0;
//                RenderTimeAccumCounter = 0;
//            }
//        }

        public string CollectIncludes(DataModel model)
        {
            IncludesCollection includes = new IncludesCollection();

            RendererFunc?.Definition?.CollectIncludeNames(includes);
            BackdropFunc?.Definition?.CollectIncludeNames(includes);

            if (IsPrimaryPass)
            {
                model.BackdropsDefinitions    .CollectIncludeNames(includes);
                model.SdfDefinitions          .CollectIncludeNames(includes);
                model.MixOpDefinitions        .CollectIncludeNames(includes);
                model.PositionOpDefinitions   .CollectIncludeNames(includes);
                model.DistanceOpDefinitions   .CollectIncludeNames(includes);
            }

            StringBuilder sb = new StringBuilder(20000);
            foreach (var include in includes)
            {
                sb.Append(include.Value);
            }
            return sb.ToString();
        }

        // TODO: can be somehow integrated with: InsertNewPositionOp, InsertNewDistanceOp
        public OperatorEntity InsertNewCameraOp(int index)
        {
            OperatorEntity opEntity = CameraOperators.CreateNewEntity();
            CameraOperators.Operators.Insert(index, opEntity);

            return opEntity;
        }


        public override void ResetPrevVal()
        {
            Enabled              .ResetPrevVal();
            IsFixed              .ResetPrevVal();
            ClearOnEveryFrame    .ResetPrevVal();
            ClearColor           .ResetPrevVal();
            ClearOpacity         .ResetPrevVal();
            AutoResetFrameCounter.ResetPrevVal();
            RendererFunc         .ResetPrevVal();
            BackdropFunc         .ResetPrevVal();
            UseTextureFiltering  .ResetPrevVal();
            CameraOperators      .ResetPrevVal();
        }

        public override bool Deserialize(XmlNode nodeThis, DataModel model)
        {
            XmlUtils.DeserializeInt     (nodeThis, "Id"             , ref Id                    );
            XmlUtils.DeserializeString  (nodeThis, "Name"           , ref Name.Val              );
            XmlUtils.DeserializeBool    (nodeThis, "IsFixed"        , ref IsFixed.Val           );

            XmlUtils.DeserializeInt     (nodeThis, "Id"                     , ref Id                        );
            XmlUtils.DeserializeString  (nodeThis, "Name"                   , ref Name.Val                  );
            XmlUtils.DeserializeBool    (nodeThis, "IsPrimaryPass"          , ref IsPrimaryPass             );
            XmlUtils.DeserializeBool    (nodeThis, "ForceClear"             , ref ForceClear                );
            XmlUtils.DeserializeBool    (nodeThis, "Enabled"                , ref Enabled.Val               );
            XmlUtils.DeserializeBool    (nodeThis, "IsFixed"                , ref IsFixed.Val               );
            XmlUtils.DeserializeBool    (nodeThis, "ClearOnEveryFrame"      , ref ClearOnEveryFrame.Val     );
            XmlUtils.DeserializeVector3 (nodeThis, "ClearColor"             , ref ClearColor.Val            );
            XmlUtils.DeserializeFloat   (nodeThis, "ClearOpacity"           , ref ClearOpacity.Val          );
            XmlUtils.DeserializeBool    (nodeThis, "AutoResetFrameCounter"  , ref AutoResetFrameCounter.Val );
            XmlUtils.DeserializeBool    (nodeThis, "UseTextureFiltering"    , ref UseTextureFiltering.Val   );

            RendererFunc.Deserialize(nodeThis, this, null);

            XmlNode? nodeBackdrop = nodeThis.SelectSingleNode("Backdrop");
            if (nodeBackdrop != null)
            {
                // TODO:
                // wyjaśnić sprawę z "this" przekazywanym jako parentObject.
                // Czy to tylko do debugowego komunikatu, czy coś więcej?
                BackdropFunc.Deserialize(nodeBackdrop, this, null);
            }

            XmlNode? nodeCameraOperators = nodeThis.SelectSingleNode("CameraOperators");
            if (nodeCameraOperators != null)
            {
                CameraOperators.Deserialize(nodeCameraOperators, null, null);
            }

            return true;
        }

        public override void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlNode nodeThis = XmlUtils.AddNode(xmlDoc, parent, "RenderPassData");

            XmlUtils.AddNodeInt     (xmlDoc, nodeThis, "Id"                     , Id                        );
            XmlUtils.AddNodeString  (xmlDoc, nodeThis, "Name"                   , Name.Val                  );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "IsPrimaryPass"          , IsPrimaryPass             );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "ForceClear"             , ForceClear                );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "Enabled"                , Enabled.Val               );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "IsFixed"                , IsFixed.Val               );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "ClearOnEveryFrame"      , ClearOnEveryFrame.Val     );
            XmlUtils.AddNodeVector3 (xmlDoc, nodeThis, "ClearColor"             , ClearColor.Val            );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "ClearOpacity"           , ClearOpacity.Val          );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "AutoResetFrameCounter"  , AutoResetFrameCounter.Val );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "UseTextureFiltering"    , UseTextureFiltering.Val   );

            RendererFunc.Serialize(xmlDoc, nodeThis);
            XmlNode nodeBackdrop = XmlUtils.AddNode(xmlDoc, nodeThis, "Backdrop");
            BackdropFunc.Serialize(xmlDoc, nodeBackdrop);
            CameraOperators.Serialize(xmlDoc, nodeThis, "CameraOperators");
        }

    }
}
