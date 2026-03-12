//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;
using SdfGlueCore.Utils;
using System.Numerics;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes
{
    public class SdfObject : SerializableNode
    {
        public ExBool                   IsFixed                 = new ExBool(false);
        public ExBool                   UseInDistanceFunction   = new ExBool(true);
        public ExBool                   UseInMaterialsFunction  = new ExBool(true);
        public ExBool                   UseShape                = new ExBool(true);
        public ExFloatWithSignal        MaterialId              = new ExFloatWithSignal(0.0f);
        public ExFloatWithSignal        MaterialBlendFactor     = new ExFloatWithSignal(0.0f);
        public ExFloatWithSignal        BlendFactor             = new ExFloatWithSignal(0.0f);
        public SdfEntity                FunctionSdf;
        public MixOperatorEntity        FunctionMixOp;
        public OperatorsCollection      PositionOperators;  // operatory nakładane na obliczaną pozycję 3d
        public OperatorsCollection      DistanceOperators;  // operatory nakładane na obliczany dystans do powierzchni sdf

        // Parameterless constructor (for copy/paste serialization)
        public SdfObject() : this(0, "", null) { }

        public SdfObject(int id, String name, FunctionDefinition? definition) : base(id, name)
        {
            FunctionSdf = new SdfEntity();
            FunctionSdf.DefinitionName.Val      = "sdBox";
            if (definition != null)
            {
                FunctionSdf.DefinitionName.Val      = definition.FunctionName;
            }

            FunctionMixOp = new MixOperatorEntity();
            FunctionMixOp.DefinitionName.Val = "opSmoothUnion";

            PositionOperators = new OperatorsCollection("posOp");
            DistanceOperators = new OperatorsCollection("distOp");

            UseInDistanceFunction   .Val    = true;
            UseInMaterialsFunction  .Val    = true;
            UseShape                .Val    = true;
            MaterialId              .Val    = 1;


            OperatorEntity opTransform = PositionOperators.CreateNewEntity();
            opTransform.DefinitionName.Val = "opTranslate";
            opTransform.ParametersValues["offset"] = new ExVector3(Vector3.Zero);
            opTransform.Enabled.Val = false;
            PositionOperators.Operators.Add(opTransform);

            OperatorEntity opRotation = PositionOperators.CreateNewEntity();
            opRotation.DefinitionName.Val = "opRotate";
            opRotation.ParametersValues["rotation"] = new ExVector3(Vector3.Zero);
            opRotation.Enabled.Val = false;
            PositionOperators.Operators.Add(opRotation);
        }

        public delegate OperatorEntity DelegateInsertNewOperator(int index);

        // Chodzi o to, że czasem chcemy przetestować czy węzeł jest Root'em.
        // RootObj ma parenta, ale nie jest on typu SdfObject, tylko DataModel (dawniej po prostu miał Parent==null)
        // Do rozważenia - można by to zastąpić czymś w rodzaju: IsRootSdf
        public SdfObject? ParentAsSdf
        {
            get
            {
                return Parent as SdfObject;
            }
        }
        public OperatorEntity InsertNewPositionOp(int index)
        {
            OperatorEntity opEntity = PositionOperators.CreateNewEntity();
            PositionOperators.Operators.Insert(index, opEntity);

            return opEntity;
        }

        public OperatorEntity InsertNewDistanceOp(int index)
        {
            OperatorEntity opEntity = DistanceOperators.CreateNewEntity();
            DistanceOperators.Operators.Insert(index, opEntity);

            return opEntity;
        }

        public override bool Deserialize(XmlNode nodeThis, DataModel model)
        {
            XmlUtils.DeserializeInt     (nodeThis, "Id"             , ref Id            );
            XmlUtils.DeserializeString  (nodeThis, "Name"           , ref Name.Val      );
            XmlUtils.DeserializeBool    (nodeThis, "IsSelected"     , ref IsSelected    );
            XmlUtils.DeserializeBool    (nodeThis, "IsExpanded"     , ref IsExpanded    );

            XmlUtils.DeserializeBool    (nodeThis, "IsFixed"                , ref IsFixed.Val               );
            XmlUtils.DeserializeBool    (nodeThis, "UseInDistanceFunction"  , ref UseInDistanceFunction .Val              );
            XmlUtils.DeserializeBool    (nodeThis, "UseInMaterialsFunction" , ref UseInMaterialsFunction.Val              );
            XmlUtils.DeserializeBool    (nodeThis, "UseShape"               , ref UseShape.Val              );
            XmlUtils.DeserializeFloat   (nodeThis, "MaterialId"             , ref MaterialId.Val            );
            XmlUtils.DeserializeFloat   (nodeThis, "MatBlendFactor"         , ref MaterialBlendFactor.Val   );
            XmlUtils.DeserializeFloat   (nodeThis, "BlendFactor"            , ref BlendFactor.Val           );

            FunctionSdf.Deserialize(nodeThis, this, model.SdfDefinitions);

            // TODO: temporary
            FunctionMixOp.RefreshDefinitionReference(model.MixOpDefinitions);

            XmlNode? nodeMixOp = nodeThis.SelectSingleNode("MixOp");
            if (nodeMixOp != null)
            {
                FunctionMixOp.Deserialize(nodeMixOp, this, model.MixOpDefinitions);
            }

            XmlNode? nodePositionOperators = nodeThis.SelectSingleNode("PositionOperators");
            if (nodePositionOperators != null)
            {
                PositionOperators.Deserialize(nodePositionOperators, this, model.PositionOpDefinitions);
            }

            XmlNode? nodeDistanceOperators = nodeThis.SelectSingleNode("DistanceOperators");
            if (nodeDistanceOperators != null)
            {
                DistanceOperators.Deserialize(nodeDistanceOperators, this, model.DistanceOpDefinitions);
            }

            XmlNode? nodeChildren = nodeThis.SelectSingleNode("Children");
            if (nodeChildren == null)
                return false;

            // Children
            XmlNodeList? childrenList = nodeChildren.SelectNodes("SdfObject");
            if (childrenList != null)
            {
                foreach(XmlNode nodeChild in childrenList)
                {
                    SdfObject obj = new SdfObject(0, "Empty", null);
                    if (obj.Deserialize(nodeChild, model))
                    {
                        AddChild(obj);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlNode nodeThis = XmlUtils.AddNode(xmlDoc, parent, "SdfObject");

            XmlUtils.AddNodeInt     (xmlDoc, nodeThis, "Id"             , Id                    );
            XmlUtils.AddNodeString  (xmlDoc, nodeThis, "Name"           , Name.Val              );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "IsSelected"     , IsSelected            );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "IsExpanded"     , IsExpanded            );

            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "IsFixed"                , IsFixed.Val               );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "UseInDistanceFunction"  , UseInDistanceFunction .Val              );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "UseInMaterialsFunction" , UseInMaterialsFunction.Val              );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "UseShape"               , UseShape.Val              );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "MaterialId"             , MaterialId.Val            );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "MatBlendFactor"         , MaterialBlendFactor.Val   );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "BlendFactor"            , BlendFactor.Val           );

            FunctionSdf.Serialize(xmlDoc, nodeThis);

            XmlNode nodeMixOp = XmlUtils.AddNode(xmlDoc, nodeThis, "MixOp");
            FunctionMixOp.Serialize(xmlDoc, nodeMixOp);

            PositionOperators.Serialize(xmlDoc, nodeThis, "PositionOperators");

            DistanceOperators.Serialize(xmlDoc, nodeThis, "DistanceOperators");

            // Children
            XmlNode nodeChildren = XmlUtils.AddNode(xmlDoc, nodeThis, "Children");
            foreach(SdfObject child in Children)
            {
                child.Serialize(xmlDoc, nodeChildren);
            }
        }

        //public bool SetPositionOperatorParameter(string operatorId, string paramId, object paramVal)
        //{
        //    if (PositionOperators == null)
        //        return false;
        //
        //    OperatorEntity opEnt = PositionOperators.FindOperatorById(operatorId);
        //    if (opEnt == null)
        //        return false;
        //
        //    return opEnt.SetParameter(paramId, paramVal);
        //}

        public string? GetMixOperatorFunctionName(bool useV2)
        {
            if (useV2)
                return FunctionMixOp.DefinitionName.Val + "Mat";
            else
                return FunctionMixOp.DefinitionName.Val;
        }

        public Vector3 GetPosition()
        {
            if (PositionOperators == null)
                return Vector3.Zero;

            OperatorEntity? opTranslate = PositionOperators.FindOperatorById("opTranslate");
            if (opTranslate == null)
                return Vector3.Zero;

            ExVector3? exObj = opTranslate.GetParameter("offset") as ExVector3;
            if (exObj == null)
                return Vector3.Zero;

            return exObj.Val;
        }

        public void FixMaterialReference(DataModel model)
        {
            MaterialInstance? mat = model.FindMaterialById((int)MaterialId.Val);
            if (mat == null)
            {
                // default material - first on list
                MaterialId.Val = model.Materials.GetChildrenAt(0).Id;
            }
        }

        public bool CanBeUsedInDistanceFunction()
        {
            if (!UseInDistanceFunction.Val)
                return false;

            if (ParentAsSdf != null)
                return ParentAsSdf.CanBeUsedInDistanceFunction();

            return true;
        }

        public bool CanBeUsedInMaterialsFunction()
        {
            if (!UseInMaterialsFunction.Val)
                return false;

            if (ParentAsSdf != null)
                return ParentAsSdf.CanBeUsedInMaterialsFunction();

            return true;
        }

        public override void ResetPrevVal()
        {
            IsFixed                 .ResetPrevVal();
            UseInDistanceFunction   .ResetPrevVal();
            UseInMaterialsFunction  .ResetPrevVal();
            UseShape                .ResetPrevVal();
            MaterialId              .ResetPrevVal();
            MaterialBlendFactor     .ResetPrevVal();
            BlendFactor             .ResetPrevVal();
            FunctionSdf             .ResetPrevVal();
            FunctionMixOp           .ResetPrevVal();
            PositionOperators       .ResetPrevVal();
            DistanceOperators       .ResetPrevVal();
        }

    }
}
