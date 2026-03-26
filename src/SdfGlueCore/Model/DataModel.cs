//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueCore.Model.Entities;
using SdfGlueCore.Utils;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Xml;

namespace SdfGlueCore.Model
{
    public class DataModel : TreeNode
    {
        //public static readonly int      VersionMajor                = 0;
        //public static readonly int      VersionMinor                = 378;

        public static readonly bool     MaterialsBlendingEnabled    = true;
        public static readonly bool     UseCameraControllers        = true;
        public static readonly bool     ImportOldMaterials          = false;
        public static readonly bool     UseSignals                  = false;//true;
        public static readonly bool     UseDocking                  = true;
        public static readonly bool     UseMultiplePreviews         = true;
        public static readonly bool     UseMultipleCodeViews        = true;
        public static readonly bool     GenCodeCleanup              = true;

        public static readonly int      NodeIdDataModel             = -10000;
        public static readonly int      NodeIdRenderingData         = -1000;
        public static readonly int      NodeIdCameraMain            = -2000;
        public static readonly int      NodeIdMaterialsCollection   = -3000;
        public static readonly int      NodeIdSignalsCollection     = -4000;
        public static readonly int      NodeIdProjectSettings       = -5000;

        public static readonly int      NumOfPreviews               = 4;
        public static readonly int      NumOfCodeViews              = 4;

        public delegate void OnValueChanged();

        public static StringBuilder     OutputLog                   = new StringBuilder(32000);

        public  int                     NextAvailableId             = 1;
        public  int                     NextAvailableMaterialId     = 1;
        public  int                     NextAvailableSignalId       = 1;
        public  int                     NextAvailableRenderPassId   = 1;

        public  ProjectSettings         ProjSettings;
        public  RenderingData           RenderingSysData;

        // time step managment
        public double                   LastDeltaTime           = 0.0;
        public double                   CurrentTime             = 0.0;
        public int                      CurrentFrame            = 0;
        public double                   AvgRenderTime           = 0.0;
        public double                   Fps                     = 0.0;
        public double                   FpsAccumTime            = 0.0;
        public int                      FpsAccumFrames          = 0;
        public bool                     IsPlaying               = true;
        public bool                     UseConstTimeStep        = false;    // TODO: needs polishing

        // root object
        public  SdfObject?              SdfRoot                 = null;

        // editor helpers
        public  String?                 ProjectFilePath         = null;
        public  TreeNode?               SelectedNode            = null;
        public  TreeNode?               ImportantNodeToSelect   = null;
        public  RenderPassData?         LastSelectedRPass       = null;
        public  TreeNode?               NodeToDelete            = null;
        public  TreeNode?               NodeToMoveUp            = null;
        public  TreeNode?               NodeToMoveDown          = null;

        // Global settings
        public  GlobalConfig            Config                  = new GlobalConfig();

        // Camera (polar-style)
        public  CameraData              CameraDat               = new CameraData();

        // Ray marching
        public int                      MarchingMaxSteps                = 100;
        public  float                   MarchingMinDist                 = 0.01f;
        public  float                   MarchingMaxDist                 = 50.0f;

        public  MaterialsCollection         Materials                   = new MaterialsCollection();
        public  SignalsCollection           Signals                     = new SignalsCollection();

        public FunctionDefinitionsSet   Renderers               = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   BackdropsDefinitions    = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   CameraControllers       = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   SdfDefinitions          = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   MixOpDefinitions        = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   PositionOpDefinitions   = new FunctionDefinitionsSet();
        public FunctionDefinitionsSet   DistanceOpDefinitions   = new FunctionDefinitionsSet();

        //public  CodeGenResult? LastGenCode
        //{
        //    get
        //    {
        //        RenderPassData? rp = GetRPassDataForPreview();
        //        if (rp == null)
        //            return null;
        //
        //        return rp.LastGenCode;
        //    }
        //}

        //public string? LastGenCodeUnity
        //{
        //    get
        //    {
        //        RenderPassData? rp = GetRPassDataForPreview();
        //        if (rp == null)
        //            return null;
        //
        //        return rp.LastGenCodeUnity;
        //    }
        //}

        public static string GetAppVersion()
        {
            //return Assembly
            //.GetEntryAssembly()?
            //.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            //?.InformationalVersion
            //?? "unknown";

            String version = Assembly.GetExecutingAssembly()?.GetName().Version?.ToString() ?? "";

            return version;
        }

        public DataModel() : base(NodeIdDataModel, "Project")
        {
            ProjSettings        = new ProjectSettings(this);
            //RenderingSysData    = new RenderingData(this); // this is done in InitDefaultData()

            ReloadDefinitions();

            Config.InitDefault();

            InitDefaultData();
            InitDefaultSdfObjects();

            RefreshDefinitions();

            RenderingSysData.RefreshDefinitionReference(Renderers, BackdropsDefinitions,  CameraControllers);

            RefreshMaterialDefinition();


            CreateEmptyProjectHierarchy();

            ResetPrevValRecursive();
        }

        public void CreateEmptyProjectHierarchy()
        {
            EmptyChildrenList();

            //AddChild(ProjSettings);
            AddChild(RenderingSysData);
            AddChild(CameraDat);
            AddChild(Materials);
            if (UseSignals)
                AddChild(Signals);

            if (SdfRoot != null)
                AddChild(SdfRoot);

            //Materials.IsExpanded = false;
        }

        private void InitDefaultData()
        {
            UndoSystem.UndoManager.Instance.ClearAll();

            NextAvailableId             = 1;
            NextAvailableMaterialId     = 1;
            NextAvailableSignalId       = 1;
            NextAvailableRenderPassId   = 1;

            SelectedNode            = null;
            ImportantNodeToSelect   = null;
            LastSelectedRPass       = null;
            NodeToDelete            = null;
            NodeToMoveUp            = null;
            NodeToMoveDown          = null;

            //---------------------------------------------------------------
            // Materials
            Materials = new MaterialsCollection();

            MaterialInstance defaultMat1 = AddNewMaterial("Material 1");
            defaultMat1.MaterialProps.SetParameterVec3("colorAmbient"   , new Vector3(0.2f, 0.2f, 0.2f) , true);
            defaultMat1.MaterialProps.SetParameterVec3("colorDiffuse"   , 0.4f * new Vector3(1.0f)      , true);
            defaultMat1.MaterialProps.SetParameterVec3("colorSpecular"  , 0.08f * new Vector3(1.0f)     , true);
            defaultMat1.MaterialProps.SetParameterFloat("shininess"     , 10.0f, true);

            MaterialInstance defaultMat2 = AddNewMaterial("Material 2");
            defaultMat2.MaterialProps.SetParameterVec3("colorDiffuse", new Vector3(0.3f, 0.7f, 0.9f), true);

            MaterialInstance defaultMat3 = AddNewMaterial("Material 3");
            defaultMat3.MaterialProps.SetParameterVec3("colorDiffuse", new Vector3(0.3f, 1.0f, 0.0f), true);

            MaterialInstance defaultMat4 = AddNewMaterial("Material 4");
            defaultMat4.MaterialProps.SetParameterVec3("colorDiffuse", new Vector3(1.0f, 1.0f, 1.0f), true);

            MaterialInstance defaultMat5 = AddNewMaterial("Material 5");
            defaultMat5.MaterialProps.SetParameterVec3("colorDiffuse", new Vector3(0.0f, 0.0f, 0.0f), true);

            // Project settings
            ProjSettings = new ProjectSettings(this);

            // Rendering
            RenderingSysData = new RenderingData(this);
            RenderingSysData.RefreshDefinitionReference(Renderers, BackdropsDefinitions, CameraControllers);

            if (UseSignals)
            {
                Signals = new SignalsCollection();
                AddNewSignal("Signal 1");
                AddNewSignal("Signal 2");
                AddNewSignal("Signal 3");
            }


            CreateEmptyProjectHierarchy();
        }

        public void SetDefaultRenderPass()
        {
            //if (RenderingSysData == null)
            //{
            //    Console.WriteLine("WARNING: SetDefaultRenderPass() failed.");
            //    return;
            //}

            if (RenderingSysData.GetChildrenCount() == 0)
            {
                Console.WriteLine("WARNING: SetDefaultRenderPass() failed.");
                return;
            }

            LastSelectedRPass   = RenderingSysData.GetChildrenAt(0) as RenderPassData;
        }

        private void InitDefaultSdfObjects()
        {
            //---------------------------------------------------------------
            // Objects

            SdfRoot = new SdfObject(NextAvailableId, "Root", null);
            NextAvailableId++;

            SdfObject box = new SdfObject(NextAvailableId, "Object1", null);

            SdfRoot.AddChild(box);
            NextAvailableId++;

            SdfRoot.FunctionSdf.DefinitionName.Val  = "sdPlane";
            SdfRoot.BlendFactor.Val     = 0.0f;

            box.FunctionSdf.DefinitionName.Val  = "sdBox";
            box.FunctionSdf.ParametersValues["dim"] = new ExVector3(new System.Numerics.Vector3(0.5f, 0.5f, 0.5f));
            box.BlendFactor.Val     = 0.5f;

            box.MaterialId.Val = 2;//"Material 2"
            box.MaterialBlendFactor.Val = 0.5f;

            OperatorEntity? opTranslate = box.PositionOperators.FindOperatorById("opTranslate");
            if (opTranslate != null)
            {
                opTranslate.Enabled.Val = true;
                opTranslate.SetParameterVec3("offset", new System.Numerics.Vector3(0.0f, 0.5f, 0.0f));
            }

            OperatorEntity? opRotate = box.PositionOperators.FindOperatorById("opRotate");
            if (opRotate != null)
            {
                opRotate.Enabled.Val = true;
                opRotate.SetParameterVec3("rotation", new System.Numerics.Vector3(0.0f, 45.0f * Utils.GMath.DegToRad, 0.0f));
            }
        }


        public void ReloadDefinitions()
        {
            Renderers               .Reload("Functions/Renderers/");
            BackdropsDefinitions    .Reload("Functions/Backdrops/");
            CameraControllers       .Reload("Functions/CameraOperators/");
            SdfDefinitions          .Reload("Functions/Sdf/");
            MixOpDefinitions        .Reload("Functions/MixOperators/");
            PositionOpDefinitions   .Reload("Functions/PositionOperators/");
            DistanceOpDefinitions   .Reload("Functions/DistanceOperators/");
        }


        public bool Deserialize(XmlDocument xmlDoc)
        {
            InitDefaultData();

            // TODO: na razie można tak zrobić - dopóki nie ma serializacji/deserializacji render passów
            RenderPassData? renderPassForMaterials = GetRenderPassForMaterials();
            if (renderPassForMaterials == null)
                return false;

            XmlNode? nodeData = xmlDoc.SelectSingleNode("Data");
            if (nodeData == null)
                return false;

            if (!XmlUtils.DeserializeInt(nodeData, "NextAvailableId", ref NextAvailableId))
                return false;

            XmlUtils.DeserializeInt(nodeData, "NextAvailableMaterialId", ref NextAvailableMaterialId);
            XmlUtils.DeserializeInt(nodeData, "NextAvailableRenderPassId", ref NextAvailableRenderPassId);

            // project settings
            XmlNode? nodeProjectSettings = nodeData.SelectSingleNode("ProjectSettings");
            if (nodeProjectSettings != null)
            {
                ProjSettings = new ProjectSettings(this);
                ProjSettings.Deserialize(nodeProjectSettings);
            }

            // camera
            CameraDat = new CameraData();
            XmlNode? nodeCamera = nodeData.SelectSingleNode("Camera");
            if (nodeCamera != null)
            {
                CameraDat.Deserialize(nodeCamera);
            }

            // materials
            XmlNode? nodeMaterials = nodeData.SelectSingleNode("Materials");
            if (nodeMaterials != null)
            {
                Materials = new MaterialsCollection();
                XmlNodeList? materialsList = nodeMaterials.SelectNodes("Material");
                if (materialsList != null)
                {
                    foreach(XmlNode node in materialsList)
                    {
                        MaterialInstance mat = new MaterialInstance();

                        // od razu odświeżamy definicję - to jest potrzebne przy deserializacji
                        mat.MaterialProps.Definition = renderPassForMaterials?.RendererFunc?.Definition;

                        mat.Deserialize(node, this);
                        Materials.AddChild(mat);
                    }
                }
            }

            XmlNode? nodeSdfObject = nodeData.SelectSingleNode("SdfObject");
            if (nodeSdfObject != null)
            {
                SdfObject rootObj = new SdfObject(0, "Empty", null);
                if (rootObj.Deserialize(nodeSdfObject, this))
                {
                    SdfRoot = rootObj;
                }
            }

            // render passes
            XmlNode? nodeRenderingSysData = nodeData.SelectSingleNode("RenderingData");
            if (nodeRenderingSysData != null)
            {
                RenderingSysData = new RenderingData(this);
                RenderingSysData.Deserialize(nodeRenderingSysData, this);
            }

            if (SdfRoot == null)
                return false;

            return true;
        }

        public XmlDocument Serialize()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(docNode);

            XmlNode nodeData = xmlDoc.CreateElement("Data");
            xmlDoc.AppendChild(nodeData);

            //XmlUtils.AddNodeInt(xmlDoc, nodeData, "VersionMajor", VersionMajor);
            //XmlUtils.AddNodeInt(xmlDoc, nodeData, "VersionMinor", VersionMinor);
            XmlUtils.AddNodeString(xmlDoc, nodeData, "Version", GetAppVersion());

            XmlUtils.AddNodeInt(xmlDoc, nodeData, "NextAvailableId"             , NextAvailableId               );
            XmlUtils.AddNodeInt(xmlDoc, nodeData, "NextAvailableMaterialId"     , NextAvailableMaterialId       );
            XmlUtils.AddNodeInt(xmlDoc, nodeData, "NextAvailableRenderPassId"   , NextAvailableRenderPassId     );

            // project settings
            ProjSettings?.Serialize(xmlDoc, nodeData);

            // camera
            CameraDat.Serialize(xmlDoc, nodeData);

            // materials
            XmlNode nodeMaterials = xmlDoc.CreateElement("Materials");
            nodeData.AppendChild(nodeMaterials);
            foreach(MaterialInstance mat in Materials.Children)
            {
                mat.Serialize(xmlDoc, nodeMaterials);
            }

            // sdf objects
            SdfRoot?.Serialize(xmlDoc, nodeData);

            // render passes
            RenderingSysData?.Serialize(xmlDoc, nodeData);

            return xmlDoc;
        }

        public void RefreshDefinitions()
        {
            TreeNode.CallRecursive(SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                sdfObj.FunctionSdf          .RefreshDefinitionReference(SdfDefinitions);
                sdfObj.FunctionMixOp        .RefreshDefinitionReference(MixOpDefinitions);
                sdfObj.PositionOperators    .RefreshDefinitionReference(PositionOpDefinitions);
                sdfObj.DistanceOperators    .RefreshDefinitionReference(DistanceOpDefinitions);
            });

            RenderingSysData?.RefreshDefinitionReference(Renderers, BackdropsDefinitions, CameraControllers);
        }

        public bool IsAbleToRender()
        {
            if (!Config.EnableRendering)
                return false;

            //if (!String.IsNullOrEmpty(LastErrorMessage))
            //    return false;

            return true;
        }

        public int FindMaterialIdByName(string name)
        {
            if (Materials == null)
                return -1;

            foreach(MaterialInstance mat in Materials.Children)
            {
                if (name == mat.GetUniqueDisplayName())
                    return mat.Id;
            }

            return -1;
        }

        public MaterialInstance? FindMaterialById(int id)
        {
            if (Materials == null)
                return null;

            foreach(MaterialInstance mat in Materials.Children)
            {
                if (mat.Id == id)
                    return mat;
            }

            return null;
        }

        public MaterialInstance AddNewMaterial(string name)
        {
            //if (Materials == null)
            //    return null;

            MaterialInstance newMat = new MaterialInstance();
            newMat.Id           = NextAvailableMaterialId;
            newMat.Name.Val     = name;
            newMat.MaterialProps.SetParameterVec3("colorAmbient"   , new Vector3(0.2f, 0.2f, 0.2f)  , true);
            newMat.MaterialProps.SetParameterVec3("colorDiffuse"   , new Vector3(0.5f, 0.5f, 0.5f)  , true);

            NextAvailableMaterialId++;

            Materials.AddChild(newMat);

            return newMat;
        }

        // funkcja usuwa referencje na nieistniejące materiały
        // (jest to potrzebne np. po usunięciu materiału, lub po załadowaniu starego pliku)
        public void FixMaterialsReferences()
        {
            if (Materials?.GetChildrenCount() == 0)
            {
                Console.WriteLine("WARNING: Materials list should never be empty!");
                return;
            }

            TreeNode.CallRecursive(SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                sdfObj?.FixMaterialReference(this);
            });
        }

        public SignalInstance? AddNewSignal(string name)
        {
            if (Signals == null)
                return null;

            SignalInstance newInst = new SignalOscillator();
            newInst.Id           = NextAvailableSignalId;
            newInst.Name.Val     = name;

            NextAvailableSignalId++;

            Signals.AddChild(newInst);

            return newInst;
        }

        public RenderPassData? GetFinalRPass()
        {
            int count = RenderingSysData?.GetChildrenCount()??0;
            if (count == 0)
                return null;

            // iterujemy od końca, szukamy włączonego
            for(int i=count-1; i>=0; i--)
            {
                RenderPassData? pass = RenderingSysData?.GetChildrenAt(i) as RenderPassData;
                if (pass == null)
                    continue;
                if (pass.Enabled.Val)
                    return pass;
            }

            return null;
        }

        public RenderPassData? GetFirstPrimaryRPass()
        {
            int count = RenderingSysData?.GetChildrenCount()??0;
            if (count == 0)
                return null;

            for(int i=0; i<count; i++)
            {
                RenderPassData? pass = RenderingSysData?.GetChildrenAt(i) as RenderPassData;
                if (pass == null)
                    continue;
                if (pass.IsPrimaryPass)
                    return pass;
            }

            return null;
        }

        public RenderPassData? GetRPassDataForPreview()
        {
            if (UseMultiplePreviews)
                return GetFinalRPass();

            return Config.PreviewLastSelectedPass ? LastSelectedRPass : GetFinalRPass();
        }

        public void ResetFrameCounter()
        {
            CurrentFrame = 0;
        }

        public void ResetTime()
        {
            CurrentTime = 0.0;
        }

        public void TimeStepBack()
        {
            CurrentTime -= GlobalConfig.ConstTimeStep;
            if (CurrentTime < 0.0)
                CurrentTime = 0.0;
        }

        public void TimeStepForward()
        {
            CurrentTime += GlobalConfig.ConstTimeStep;
        }

        private void UpdateTime(double deltaTime)
        {
            LastDeltaTime = deltaTime;

            if (IsPlaying)
            {
                CurrentTime += deltaTime;
                CurrentFrame++;
            }

            FpsAccumTime += deltaTime;
            FpsAccumFrames++;

            double fpsCheckPeriod = 0.5;

            if (FpsAccumTime > fpsCheckPeriod)
            {
                AvgRenderTime = 0.0;
                Fps = 0.0;
                if (FpsAccumFrames > 0)
                {
                    AvgRenderTime = FpsAccumTime / FpsAccumFrames;
                    Fps = 1.0 / AvgRenderTime;
                }

                FpsAccumTime -= fpsCheckPeriod;
                FpsAccumFrames = 0;
            }
        }

        public void Update(double deltaTime)
        {
            UpdateTime(deltaTime);

            if (UseSignals)
                Signals?.Update(deltaTime);
        }

        public override void ResetPrevVal()
        {
        }

        public void ResetPrevValRecursive()
        {
            TreeNode.CallRecursive(this, delegate(TreeNode node)
            {
                node.ResetPrevVal();
            });
        }

        public RenderPassData? GetRenderPassForMaterials()
        {
            //if (!UseCustomMaterialProperties)
            //    return null;

            // TODO: możliwe że do usprawnienia w przyszłości - na razie bazujemy na pierwszym znalezionym primary pass
            RenderPassData? primRp = GetFirstPrimaryRPass();
            if (primRp == null)
                return null;

            return primRp;
        }

        public void RefreshMaterialDefinition()
        {
            RenderPassData? rp = GetRenderPassForMaterials();
            if (rp == null)
                return;

            Materials?.RefreshDefinitionReference(rp?.RendererFunc?.Definition);
        }

    }

    public class IncludesCollection : Dictionary<string, string> {}
}
