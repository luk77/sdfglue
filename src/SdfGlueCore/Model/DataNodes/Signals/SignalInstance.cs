//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    // A signal: value of the source processed by the list of operators.
    // Parameters refer to signals by Id (ExFloatWithSignal.SignalId).
    public class SignalInstance : SerializableNode
    {
        public static readonly string   DefaultSourceType   = "Oscillator";

        public  ExBool                  Enabled             = new ExBool(true);
        public  SignalSource            Source;
        public  List<SignalOperator>    Operators           = new List<SignalOperator>();

        // runtime only
        public  SignalHistory           History             = new SignalHistory();

        private float                   value_              = 0.0f;
        private int                     evalUpdateIndex_    = -1;
        private bool                    isEvaluating_       = false;
        private bool                    cycleReported_      = false;

        public SignalInstance() : base(0, "Signal")
        {
            Source = SignalTypesRegistry.CreateSource(DefaultSourceType) ?? new SignalSrcConstant();
        }

        public float GetCurrentValue()
        {
            return value_;
        }

        public override void ResetPrevVal()
        {
            base.ResetPrevVal();

            Enabled.ResetPrevVal();
            Source.ResetPrevVal();
            foreach(SignalOperator op in Operators)
                op.ResetPrevVal();
        }

        // Clears the state of stateful operators (e.g. after a jump of the project time)
        public void ResetState()
        {
            Source.ResetState();
            foreach(SignalOperator op in Operators)
                op.ResetState();
        }

        // The value is computed once per update (ctx.UpdateIndex); references to other signals
        // are evaluated on demand, a reference cycle gives 0
        public float Evaluate(SignalContext ctx)
        {
            if (evalUpdateIndex_ == ctx.UpdateIndex)
                return value_;

            if (isEvaluating_)
            {
                if (!cycleReported_)
                {
                    Console.WriteLine("WARNING: Signals: reference cycle detected (signal: {0}, id: {1}).", Name.Val, Id);
                    cycleReported_ = true;
                }
                return 0.0f;
            }

            isEvaluating_ = true;

            bool useRealTime = ctx.UseRealTime;
            ctx.UseRealTime = Source.UsesRealTime;

            float val = Source.Evaluate(ctx);
            foreach(SignalOperator op in Operators)
            {
                if (op.Enabled.Val)
                    val = op.Process(val, ctx);
            }

            ctx.UseRealTime = useRealTime;

            if (!float.IsFinite(val))
                val = 0.0f;

            isEvaluating_       = false;
            evalUpdateIndex_    = ctx.UpdateIndex;
            value_              = val;

            return value_;
        }

        public void SetSource(SignalSource source)
        {
            Source = source;
        }

        public override bool Deserialize(XmlNode nodeThis, IAbstractDocument model)
        {
            XmlUtils.DeserializeInt     (nodeThis, "Id"         , ref Id            );
            XmlUtils.DeserializeString  (nodeThis, "Name"       , ref Name.Val      );
            XmlUtils.DeserializeBool    (nodeThis, "Enabled"    , ref Enabled.Val   );

            XmlNode? nodeSource = nodeThis.SelectSingleNode("Source");
            if (nodeSource != null)
            {
                string? typeName = XmlUtils.LoadAttributeAsString(nodeSource, "type", null);
                SignalSource? source = SignalTypesRegistry.CreateSource(typeName);
                if (source != null)
                {
                    source.DeserializeParameters(nodeSource);
                    Source = source;
                }
                else
                {
                    Console.WriteLine("WARNING: Unknown signal source type: {0} (signal: {1}).", typeName, Name.Val);
                }
            }

            Operators.Clear();
            XmlNodeList? operatorsList = nodeThis.SelectNodes("Operators/Operator");
            if (operatorsList != null)
            {
                foreach(XmlNode nodeOp in operatorsList)
                {
                    string? typeName = XmlUtils.LoadAttributeAsString(nodeOp, "type", null);
                    SignalOperator? op = SignalTypesRegistry.CreateOperator(typeName);
                    if (op == null)
                    {
                        Console.WriteLine("WARNING: Unknown signal operator type: {0} (signal: {1}).", typeName, Name.Val);
                        continue;
                    }

                    op.Enabled.Val = XmlUtils.LoadAttributeAsBool(nodeOp, "enabled", true);
                    op.DeserializeParameters(nodeOp);
                    Operators.Add(op);
                }
            }

            return true;
        }

        public override void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlNode nodeThis = XmlUtils.AddNode(xmlDoc, parent, "Signal");

            XmlUtils.AddNodeInt     (xmlDoc, nodeThis, "Id"         , Id                );
            XmlUtils.AddNodeString  (xmlDoc, nodeThis, "Name"       , Name.Val          );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "Enabled"    , Enabled.Val       );

            XmlElement nodeSource = XmlUtils.AddNode(xmlDoc, nodeThis, "Source");
            XmlUtils.AddAtributeString(nodeSource, "type", Source.TypeName);
            Source.SerializeParameters(xmlDoc, nodeSource);

            XmlNode nodeOperators = XmlUtils.AddNode(xmlDoc, nodeThis, "Operators");
            foreach(SignalOperator op in Operators)
            {
                XmlElement nodeOp = XmlUtils.AddNode(xmlDoc, nodeOperators, "Operator");
                XmlUtils.AddAtributeString(nodeOp, "type"   , op.TypeName);
                XmlUtils.AddAtributeString(nodeOp, "enabled", op.Enabled.Val ? "true" : "false");
                op.SerializeParameters(xmlDoc, nodeOp);
            }
        }
    }
}
