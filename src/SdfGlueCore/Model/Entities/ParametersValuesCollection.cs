//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.Entities
{
    //public class ParametersValuesCollection : Dictionary<string, object> { }
    //public class ParametersValuesCollection : Dictionary<string, ISimpleType> { }
    public class ParametersValuesCollection : Dictionary<string, ISimpleType>
    {
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

                // Logika jest taka:
                // Przed wczytaniem w ParametersValues są defaultowe parametry zgodne z obecną definicją.
                // Przy wczycie uzupełniamy wszystko o ile jest zgodność typów. 
                // Jeśli typy się nie zgadzają, to znaczy że definicja uległa zmianie i lepiej taki parametr pominąć (zostawiając default).
                // Wczytujemy także nadmiarowe parametry, które są np. pozostałością po dynamicznej zmianie typu obiektu.
                // Dzięki temu jak ktoś wróci do starego typu to będzie miał stare dane ustawione.
                bool canLoad = false;
                if (this.ContainsKey(paramName))
                {
                    Type currParamValueType = this[paramName].GetValueType();
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
                        this[paramName] = new ExFloatWithSignal(readVal);
                    }
                    else if ((paramType == "int"))
                    {
                        int readVal = 0;
                        XmlUtils.TryParseInt(nodeParam.InnerText, ref readVal);
                        this[paramName] = new ExInt(readVal);
                    }
                    else if ((paramType == "vec2"))
                    {
                        System.Numerics.Vector2 readVal = System.Numerics.Vector2.Zero;
                        XmlUtils.TryParseVector2(nodeParam.InnerText, ref readVal);
                        this[paramName] = new ExVector2(readVal);
                    }
                    else if ((paramType == "vec3"))
                    {
                        System.Numerics.Vector3 readVal = System.Numerics.Vector3.Zero;
                        XmlUtils.TryParseVector3(nodeParam.InnerText, ref readVal);
                        this[paramName] = new ExVector3(readVal);
                    }
                    else if ((paramType == "vec4"))
                    {
                        System.Numerics.Vector4 readVal = System.Numerics.Vector4.Zero;
                        XmlUtils.TryParseVector4(nodeParam.InnerText, ref readVal);
                        this[paramName] = new ExVector4(readVal);
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
                    XmlUtils.AddAtributeString(nodeParam, "name", keyVal.Key);
                    XmlUtils.AddAtributeString(nodeParam, "type", "float");
                }
                else if (param.GetType() == typeof(ExInt))
                {
                    ExInt val = (ExInt)param;
                    XmlElement nodeParam = XmlUtils.AddNodeInt(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", keyVal.Key);
                    XmlUtils.AddAtributeString(nodeParam, "type", "int");
                }
                else if (param.GetType() == typeof(ExVector2))
                {
                    ExVector2 val = (ExVector2)param;
                    XmlElement nodeParam = XmlUtils.AddNodeVector2(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", keyVal.Key);
                    XmlUtils.AddAtributeString(nodeParam, "type", "vec2");
                }
                else if (param.GetType() == typeof(ExVector3))
                {
                    ExVector3 val = (ExVector3)param;
                    XmlElement nodeParam = XmlUtils.AddNodeVector3(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", keyVal.Key);
                    XmlUtils.AddAtributeString(nodeParam, "type", "vec3");
                }
                else if (param.GetType() == typeof(ExVector4))
                {
                    ExVector4 val = (ExVector4)param;
                    XmlElement nodeParam = XmlUtils.AddNodeVector4(xmlDoc, nodeParametersValues, "PValue"    , val.Val);
                    XmlUtils.AddAtributeString(nodeParam, "name", keyVal.Key);
                    XmlUtils.AddAtributeString(nodeParam, "type", "vec4");
                }
                else
                {
                    Console.WriteLine("WARNING: Unable to serialize parameter: {0}", keyVal.Key);
                }
            }
        }
    }
}
