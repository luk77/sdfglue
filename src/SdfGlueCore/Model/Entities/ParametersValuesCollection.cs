//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Utils;
using System.Xml;
using SingleDocAppCore.Model.DataNodes;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes.Signals;

namespace SdfGlueCore.Model.Entities
{
    //public class ParametersValuesCollection : Dictionary<string, object> { }
    //public class ParametersValuesCollection : Dictionary<string, ISimpleType> { }
    // Keys are "type:name" (e.g. "vec3:radius", see FunctionDefParameter.ParameterKey) - build them only with MakeKey().
    // In the project file the name and the type are stored in separate attributes (<PValue name="radius" type="vec3">).
    public class ParametersValuesCollection : Dictionary<string, ISimpleType>
    {
        private const char KeySeparator = ':';

        public static string MakeKey(SdfParamType type, string parameterName)
        {
            return MakeKey(FunctionDefParameter.SdfTypeToString(type), parameterName);
        }

        private static string MakeKey(string typeName, string parameterName)
        {
            return typeName + KeySeparator + parameterName;
        }

        // Returns the parameter name part of the key
        public static string GetParameterName(string key)
        {
            int separatorIndex = key.IndexOf(KeySeparator);
            return separatorIndex >= 0 ? key.Substring(separatorIndex + 1) : key;
        }

        internal void Deserialize(XmlNode nodeParametersValues, TreeNode? parentObject)
        {
            XmlNodeList? paramsList = nodeParametersValues.SelectNodes("PValue");
            if (paramsList == null)
                return;

            foreach(XmlNode nodeParam in paramsList)
            {
                string? paramName = XmlUtils.LoadAttributeAsString(nodeParam, "name", null);
                string? paramType = XmlUtils.LoadAttributeAsString(nodeParam, "type", null);
                if ((paramName == null) || (paramType == null))
                    continue;

                string paramKey = MakeKey(paramType, paramName);

                // The logic is:
                // Before loading, ParametersValues contains default parameters matching the current definition.
                // While loading, everything is filled in as long as the types match.
                // The key contains the type, so a value with a different type than in the definition (the definition has changed)
                // goes under another key as an extra parameter, and the default stays.
                // Extra parameters are loaded too, e.g. leftovers from a dynamic change of the object type.
                // Thanks to that, switching back to the old type restores the old values.
                bool canLoad = false;
                if (this.ContainsKey(paramKey))
                {
                    Type currParamValueType = this[paramKey].GetValueType();
                    if (((paramType == "float") && (currParamValueType == typeof(float)))                   ||
                        ((paramType == "int")   && (currParamValueType == typeof(int)))                     ||
                        ((paramType == "vec2")  && (currParamValueType == typeof(System.Numerics.Vector2))) ||
                        ((paramType == "vec3")  && (currParamValueType == typeof(System.Numerics.Vector3))) ||
                        ((paramType == "vec4")  && (currParamValueType == typeof(System.Numerics.Vector4))) )
                    {
                        canLoad = true;
                    }
                    else
                    {
                        if (parentObject != null)
                            Console.WriteLine("WARNING: Incompatible parameter. Object:{0}, Id:{1}, Parameter name:{2}", parentObject.Name.Val, parentObject.Id, paramName);
                        else
                            Console.WriteLine("WARNING: Incompatible parameter. Parameter name:{0}", paramName);

                        continue;
                    }
                }
                else
                {
                    canLoad = true;
                }

                if (canLoad)
                {
                    if ((paramType == "float"))
                    {
                        float readVal = 0.0f;
                        XmlUtils.TryParseFloat(nodeParam.InnerText, ref readVal);
                        ExFloatWithSignal exObj = new ExFloatWithSignal(readVal);
                        SignalBinding.DeserializeBinding(nodeParam, exObj);
                        this[paramKey] = exObj;
                    }
                    else if ((paramType == "int"))
                    {
                        int readVal = 0;
                        XmlUtils.TryParseInt(nodeParam.InnerText, ref readVal);
                        this[paramKey] = new ExInt(readVal);
                    }
                    else if ((paramType == "vec2"))
                    {
                        System.Numerics.Vector2 readVal = System.Numerics.Vector2.Zero;
                        XmlUtils.TryParseVector2(nodeParam.InnerText, ref readVal);
                        ExVector2WithSignal exObj = new ExVector2WithSignal(readVal);
                        SignalBinding.DeserializeBinding(nodeParam, exObj);
                        this[paramKey] = exObj;
                    }
                    else if ((paramType == "vec3"))
                    {
                        System.Numerics.Vector3 readVal = System.Numerics.Vector3.Zero;
                        XmlUtils.TryParseVector3(nodeParam.InnerText, ref readVal);
                        ExVector3WithSignal exObj = new ExVector3WithSignal(readVal);
                        SignalBinding.DeserializeBinding(nodeParam, exObj);
                        this[paramKey] = exObj;
                    }
                    else if ((paramType == "vec4"))
                    {
                        System.Numerics.Vector4 readVal = System.Numerics.Vector4.Zero;
                        XmlUtils.TryParseVector4(nodeParam.InnerText, ref readVal);
                        ExVector4WithSignal exObj = new ExVector4WithSignal(readVal);
                        SignalBinding.DeserializeBinding(nodeParam, exObj);
                        this[paramKey] = exObj;
                    }
                }
            }
        }

        public virtual void Serialize(XmlDocument xmlDoc, XmlNode nodeParametersValues)
        {
            foreach(var keyVal in this)
            {
                //XmlNode nodeParam = XmlUtils.AddNode(xmlDoc, nodeParametersValues, keyVal.Key);
                object param = keyVal.Value;
                if (param.GetType() == typeof(ExFloat) ||
                    param.GetType() == typeof(ExFloatWithSignal) )
                {
                    ExFloat val = (ExFloat)param;
                    XmlElement nodeParam = XmlUtils.AddNodeFloat(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", GetParameterName(keyVal.Key));
                    XmlUtils.AddAtributeString(nodeParam, "type", "float");
                    SignalBinding.SerializeBinding(nodeParam, val);
                }
                else if (param.GetType() == typeof(ExInt))
                {
                    ExInt val = (ExInt)param;
                    XmlElement nodeParam = XmlUtils.AddNodeInt(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", GetParameterName(keyVal.Key));
                    XmlUtils.AddAtributeString(nodeParam, "type", "int");
                }
                else if (param.GetType() == typeof(ExVector2) ||
                         param.GetType() == typeof(ExVector2WithSignal) )
                {
                    ExVector2 val = (ExVector2)param;
                    XmlElement nodeParam = XmlUtils.AddNodeVector2(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", GetParameterName(keyVal.Key));
                    XmlUtils.AddAtributeString(nodeParam, "type", "vec2");
                    SignalBinding.SerializeBinding(nodeParam, val);
                }
                else if (param.GetType() == typeof(ExVector3) ||
                         param.GetType() == typeof(ExVector3WithSignal) )
                {
                    ExVector3 val = (ExVector3)param;
                    XmlElement nodeParam = XmlUtils.AddNodeVector3(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", GetParameterName(keyVal.Key));
                    XmlUtils.AddAtributeString(nodeParam, "type", "vec3");
                    SignalBinding.SerializeBinding(nodeParam, val);
                }
                else if (param.GetType() == typeof(ExVector4) ||
                         param.GetType() == typeof(ExVector4WithSignal) )
                {
                    ExVector4 val = (ExVector4)param;
                    XmlElement nodeParam = XmlUtils.AddNodeVector4(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", GetParameterName(keyVal.Key));
                    XmlUtils.AddAtributeString(nodeParam, "type", "vec4");
                    SignalBinding.SerializeBinding(nodeParam, val);
                }
                else
                {
                    Console.WriteLine("WARNING: Unable to serialize parameter: {0}", keyVal.Key);
                }
            }
        }
    }
}
