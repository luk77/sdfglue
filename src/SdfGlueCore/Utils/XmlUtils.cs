//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;
using System.Text;
using System.Xml;

namespace SdfGlueCore.Utils
{
    public class XmlUtils
    {
        public static string? LoadFileAsString(string path)
        {
            try
            {
                using (var sr = new StreamReader(path, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("WARNING: " + ex.ToString());
                return null;
            }
        }

        public static XmlElement AddNode(XmlDocument xmlDoc, XmlNode parent, string name)
        {
            XmlElement node = xmlDoc.CreateElement(name);
            parent.AppendChild(node);
            return node;
        }

        public static XmlElement AddNodeBool(XmlDocument xmlDoc, XmlNode parent, string name, bool val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = val.ToString(CultureInfo.InvariantCulture);
            return node;
        }

        public static XmlElement AddNodeInt(XmlDocument xmlDoc, XmlNode parent, string name, int val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = val.ToString(CultureInfo.InvariantCulture);
            return node;
        }

        public static XmlElement AddNodeFloat(XmlDocument xmlDoc, XmlNode parent, string name, float val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = val.ToString(CultureInfo.InvariantCulture);
            return node;
        }

        public static XmlElement AddNodeDouble(XmlDocument xmlDoc, XmlNode parent, string name, double val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = val.ToString(CultureInfo.InvariantCulture);
            return node;
        }

        public static XmlElement AddNodeString(XmlDocument xmlDoc, XmlNode parent, string name, string? val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = val??"";
            return node;
        }

        public static XmlElement AddNodeVector2(XmlDocument xmlDoc, XmlNode parent, string name, System.Numerics.Vector2 val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = String.Format("{0};{1}", 
                val.X.ToString(CultureInfo.InvariantCulture),
                val.Y.ToString(CultureInfo.InvariantCulture));
            return node;
        }

        public static XmlElement AddNodeVector3(XmlDocument xmlDoc, XmlNode parent, string name, System.Numerics.Vector3 val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = String.Format("{0};{1};{2}", 
                val.X.ToString(CultureInfo.InvariantCulture),
                val.Y.ToString(CultureInfo.InvariantCulture),
                val.Z.ToString(CultureInfo.InvariantCulture));
            return node;
        }

        public static XmlElement AddNodeVector4(XmlDocument xmlDoc, XmlNode parent, string name, System.Numerics.Vector4 val)
        {
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = String.Format("{0};{1};{2};{3}", 
                val.X.ToString(CultureInfo.InvariantCulture),
                val.Y.ToString(CultureInfo.InvariantCulture),
                val.Z.ToString(CultureInfo.InvariantCulture),
                val.W.ToString(CultureInfo.InvariantCulture));
            return node;
        }

        public static XmlElement AddNodeEnum<T>(XmlDocument xmlDoc, XmlNode parent, string name, T val) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }
            XmlElement node = AddNode(xmlDoc, parent, name);
            node.InnerText = val.ToString()!;
            return node;
        }



        public static bool TryParseBool(string text, ref bool val)
        {
            return bool.TryParse(text, out val);
        }

        public static bool TryParseInt(string text, ref int val)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out val);
        }

        public static bool TryParseFloat(string text, ref float val)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }

        public static bool TryParseDouble(string text, ref double val)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }

        public static bool TryParseVector2(string? text, ref System.Numerics.Vector2 val)
        {
            if (text == null)
                return false;

            string[] parts = text.Split(new char[] {';'});
            if (parts.Length < 2)
                return false;

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out val.X))
                return false;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out val.Y))
                return false;

            return true;
        }

        public static bool TryParseVector3(string? text, ref System.Numerics.Vector3 val)
        {
            if (text == null)
                return false;

            string[] parts = text.Split(new char[] {';'});
            if (parts.Length < 3)
                return false;

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out val.X))
                return false;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out val.Y))
                return false;

            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out val.Z))
                return false;

            return true;
        }

        public static bool TryParseVector4(string? text, ref System.Numerics.Vector4 val)
        {
            if (text == null)
                return false;

            string[] parts = text.Split(new char[] {';'});
            if (parts.Length < 4)
                return false;

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out val.X))
                return false;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out val.Y))
                return false;

            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out val.Z))
                return false;

            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out val.W))
                return false;

            return true;
        }


        public static bool DeserializeBool(XmlNode parent, string childNodeName, ref bool val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            return TryParseBool(node.InnerText, ref val);
        }

        public static bool DeserializeInt(XmlNode parent, string childNodeName, ref int val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            return TryParseInt(node.InnerText, ref val);
        }

        public static bool DeserializeFloat(XmlNode parent, string childNodeName, ref float val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            return TryParseFloat(node.InnerText, ref val);
        }

        public static bool DeserializeDouble(XmlNode parent, string childNodeName, ref double val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            return TryParseDouble(node.InnerText, ref val);
        }

        public static bool DeserializeString(XmlNode parent, string childNodeName, ref string? val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            val = node.InnerText;
            return true;
        }

        public static bool DeserializeVector2(XmlNode parent, string childNodeName, ref System.Numerics.Vector2 val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            return TryParseVector2(node.InnerText, ref val);
        }

        public static bool DeserializeVector3(XmlNode parent, string childNodeName, ref System.Numerics.Vector3 val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            return TryParseVector3(node.InnerText, ref val);
        }

        public static bool DeserializeVector4(XmlNode parent, string childNodeName, ref System.Numerics.Vector4 val)
        {
            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            return TryParseVector4(node.InnerText, ref val);
        }

        public static bool DeserializeEnum<T>(XmlNode parent, string childNodeName, ref T val)  where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            XmlNode? node = parent.SelectSingleNode(childNodeName);
            if (node == null)
                return false;

            try
            {
                val = (T)Enum.Parse(typeof(T), node.InnerText);
            }
            catch(Exception)
            {
                Console.WriteLine("WARNING: DeserializeEnum failed for: {0}, ({1})", node.InnerText, typeof(T));
                return false;
            }

            return true;
        }

        public static string? LoadAttributeAsString(XmlNode node, string attributeName, string? defaultValue)
        {
            XmlNode? attr = node.Attributes?.GetNamedItem(attributeName);
            if (attr == null)
                return defaultValue;

            return attr.Value;
        }

        public static float LoadAttributeAsFloat(XmlNode node, string attributeName, float defaultValue)
        {
            XmlNode? attr = node.Attributes?.GetNamedItem(attributeName);
            if (attr == null)
                return defaultValue;

            float val = 0.0f;
            float.TryParse(attr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out val);

            return val;
        }

        public static bool LoadAttributeAsBool(XmlNode node, string attributeName, bool defaultValue)
        {
            XmlNode? attr = node.Attributes?.GetNamedItem(attributeName);
            if (attr == null)
                return defaultValue;

            bool val = false;
            bool.TryParse(attr.Value, out val);

            return val;
        }

        public static System.Numerics.Vector2 LoadAttributeAsVec2(XmlNode node, string attributeName, System.Numerics.Vector2 defaultValue)
        {
            XmlNode? attr = node.Attributes?.GetNamedItem(attributeName);
            if (attr == null)
                return defaultValue;

            System.Numerics.Vector2 val = System.Numerics.Vector2.Zero;
            TryParseVector2(attr.Value, ref val);

            return val;
        }

        public static System.Numerics.Vector3 LoadAttributeAsVec3(XmlNode node, string attributeName, System.Numerics.Vector3 defaultValue)
        {
            XmlNode? attr = node.Attributes?.GetNamedItem(attributeName);
            if (attr == null)
                return defaultValue;

            System.Numerics.Vector3 val = System.Numerics.Vector3.Zero;
            TryParseVector3(attr.Value, ref val);

            return val;
        }

        public static System.Numerics.Vector4 LoadAttributeAsVec4(XmlNode node, string attributeName, System.Numerics.Vector4 defaultValue)
        {
            XmlNode? attr = node.Attributes?.GetNamedItem(attributeName);
            if (attr == null)
                return defaultValue;

            System.Numerics.Vector4 val = System.Numerics.Vector4.Zero;
            TryParseVector4(attr.Value, ref val);

            return val;
        }

        public static void AddAtributeString(XmlElement node, string attributeName, string attributeValue)
        {
            node.SetAttribute(attributeName, attributeValue);
        }
    }
}
