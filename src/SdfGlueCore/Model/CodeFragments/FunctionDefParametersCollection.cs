//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using SdfGlueCore.Model.Entities;
using System.Xml;
using SdfGlueCore.Model.BaseTypes;

namespace SdfGlueCore.Model.CodeFragments
{
    public class FunctionDefParametersCollection : List<FunctionDefParameter>
    {
        internal FunctionDefParameter? FindParameterByKey(string parameterKey)
        {
            foreach(FunctionDefParameter p in this)
            {
                if (p.ParameterKey == parameterKey)
                    return p;
            }
            return null;
        }

        internal ParametersValuesCollection GetDefaultParameters(ParametersValuesCollection parametersValues)
        {
            foreach(var p in this)
            {
                if (p.ParameterName == null)
                    continue;

                // If such an entry already exists in the dictionary, keep the old data.
                // Thanks to that, e.g. the 'radius' parameter is kept when the type is changed dynamically.
                // The key contains the parameter type ("float:radius"), so a kept value always has a matching type.
                if (parametersValues.ContainsKey(p.ParameterKey))
                {
                    continue;
                }
                else
                {
                    // If the definition contains this parameter, take the default from it
                    FunctionDefParameter? paramDef = FindParameterByKey(p.ParameterKey);
                    if (paramDef != null)
                    {
                        if (paramDef.DefaultVal == null)
                            continue; // this should never happen

                        //switch(p.Type)
                        //{
                        //    case SdfParamType.Float:    parametersValues[p.ParameterKey] = paramDef.DefaultVal * (paramDef.EditInDegrees ? MathUtils.DegToRad : 1.0f); break;
                        //    case SdfParamType.Int:      parametersValues[p.ParameterKey] = ((int)paramDef.DefaultVal);                             break;
                        //    case SdfParamType.Vec2:     parametersValues[p.ParameterKey] = (new System.Numerics.Vector2(paramDef.DefaultVal));     break;
                        //    case SdfParamType.Vec3:     parametersValues[p.ParameterKey] = (new System.Numerics.Vector3(paramDef.DefaultVal));     break;
                        //    case SdfParamType.Vec4:     parametersValues[p.ParameterKey] = (new System.Numerics.Vector4(paramDef.DefaultVal));     break;
                        //    //case SdfParamType.Bool:     parametersValues[p.ParameterKey] = (paramDef.DefaultVal != 0.0f); break;
                        //}

                        // conversion is done earlier
                        // this must be a copy of the wrapper object, not a copied reference!
                        parametersValues[p.ParameterKey] = paramDef.DefaultVal.Copy();
                    }
                    else
                    {
                        switch(p.Type)
                        {
                            case SdfParamType.Float:    parametersValues[p.ParameterKey] = new ExFloatWithSignal(0.0f); break;
                            case SdfParamType.Int:      parametersValues[p.ParameterKey] = new ExInt(0);      break;
                            case SdfParamType.Vec2:     parametersValues[p.ParameterKey] = new ExVector2WithSignal(System.Numerics.Vector2.Zero);      break;
                            case SdfParamType.Vec3:     parametersValues[p.ParameterKey] = new ExVector3WithSignal(System.Numerics.Vector3.Zero);      break;
                            case SdfParamType.Vec4:     parametersValues[p.ParameterKey] = new ExVector4WithSignal(System.Numerics.Vector4.Zero);      break;
                            //case SdfParamType.Bool:     parametersValues[p.ParameterKey] = new ExBool(false); break;
                        }
                    }
                }
            }

            return parametersValues;
        }

        internal void Deserialize(XmlNode nodeParent)
        {
            if (nodeParent == null)
                return;

            foreach(XmlNode p in nodeParent.ChildNodes)
            {
                if (p.NodeType == XmlNodeType.Comment)
                    continue;

                FunctionDefParameter sdfParam = new FunctionDefParameter();
                bool success = sdfParam.Deserialize(p);
                if (!success)
                    continue;

                Add(sdfParam);
            }
        }
    }
}
