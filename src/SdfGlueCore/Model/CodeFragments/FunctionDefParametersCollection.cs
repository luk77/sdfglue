//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.Entities;
using System.Xml;

namespace SdfGlueCore.Model.CodeFragments
{
    public class FunctionDefParametersCollection : List<FunctionDefParameter>
    {
        internal FunctionDefParameter? FindParameterByName(string parameterName)
        {
            foreach(FunctionDefParameter p in this)
            {
                if (p.ParameterName == parameterName)
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

                // Jesli już taki wpis występuje w słowniku to zostawiamy stare dane.
                // Dzięki temu przy dynamicznej zmianie typu przejdzie parametr 'radius'.
                if (parametersValues.ContainsKey(p.ParameterName))
                {
                    // TODO: tu trzeba by zweryfikować czy typ się zgadza, bo jeśli nie to będą problemy
                    continue;
                }
                else
                {
                    // Jeśli w definicji występuje ten parametr, to z niej bierzemy default
                    FunctionDefParameter? paramDef = FindParameterByName(p.ParameterName);
                    if (paramDef != null)
                    {
                        if (paramDef.DefaultVal == null)
                            continue; // to raczej nigdy nie powinno wystąpić

                        //switch(p.Type)
                        //{
                        //    case SdfParamType.Float:    parametersValues[p.ParameterName] = paramDef.DefaultVal * (paramDef.EditInDegrees ? MathUtils.DegToRad : 1.0f); break;
                        //    case SdfParamType.Int:      parametersValues[p.ParameterName] = ((int)paramDef.DefaultVal);                             break;
                        //    case SdfParamType.Vec2:     parametersValues[p.ParameterName] = (new System.Numerics.Vector2(paramDef.DefaultVal));     break;
                        //    case SdfParamType.Vec3:     parametersValues[p.ParameterName] = (new System.Numerics.Vector3(paramDef.DefaultVal));     break;
                        //    case SdfParamType.Vec4:     parametersValues[p.ParameterName] = (new System.Numerics.Vector4(paramDef.DefaultVal));     break;
                        //    //case SdfParamType.Bool:     parametersValues[p.ParameterName] = (paramDef.DefaultVal != 0.0f); break;
                        //}

                        // konwersja jest robiona wcześniej
                        // tu musi być kopia obiektu wrappera, a nie przeniesienie referencji!
                        parametersValues[p.ParameterName] = paramDef.DefaultVal.Copy();
                    }
                    else
                    {
                        switch(p.Type)
                        {
                            case SdfParamType.Float:    parametersValues[p.ParameterName] = new ExFloatWithSignal(0.0f); break;
                            case SdfParamType.Int:      parametersValues[p.ParameterName] = new ExInt(0);      break;
                            case SdfParamType.Vec2:     parametersValues[p.ParameterName] = new ExVector2(System.Numerics.Vector2.Zero);      break;
                            case SdfParamType.Vec3:     parametersValues[p.ParameterName] = new ExVector3(System.Numerics.Vector3.Zero);      break;
                            case SdfParamType.Vec4:     parametersValues[p.ParameterName] = new ExVector4(System.Numerics.Vector4.Zero);      break;
                            //case SdfParamType.Bool:     parametersValues[p.ParameterName] = new ExBool(false); break;
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
