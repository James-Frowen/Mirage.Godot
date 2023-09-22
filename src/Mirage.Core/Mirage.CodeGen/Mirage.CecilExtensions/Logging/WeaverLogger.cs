using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.CodeGen
{
    public class DiagnosticMessage
    {
        public Type DiagnosticType;
        public string MessageData;

        public enum Type
        {
            Error,
            Warning
        }
    }

    public class WeaverLogger : IWeaverLogger
    {
        private readonly List<DiagnosticMessage> _diagnostics = new List<DiagnosticMessage>();

        // Create Copy so private list can't be altered
        public List<DiagnosticMessage> GetDiagnostics() => _diagnostics.ToList();

        public bool EnableTrace { get; }

        public WeaverLogger(bool enableTrace)
        {
            EnableTrace = enableTrace;
        }

        public void Error(string message)
        {
            AddMessage(message, null, DiagnosticMessage.Type.Error);
        }

        public void Error(string message, MemberReference mr)
        {
            Error($"{message} (at {mr})");
        }

        public void Error(string message, MemberReference mr, SequencePoint sequencePoint)
        {
            AddMessage($"{message} (at {mr})", sequencePoint, DiagnosticMessage.Type.Error);
        }

        public void Error(string message, MethodDefinition md)
        {
            Error(message, md, md.DebugInformation.SequencePoints.FirstOrDefault());
        }


        public void Warning(string message)
        {
            AddMessage($"{message}", null, DiagnosticMessage.Type.Warning);
        }

        public void Warning(string message, MemberReference mr)
        {
            Warning($"{message} (at {mr})");
        }

        public void Warning(string message, MemberReference mr, SequencePoint sequencePoint)
        {
            AddMessage($"{message} (at {mr})", sequencePoint, DiagnosticMessage.Type.Warning);
        }

        public void Warning(string message, MethodDefinition md)
        {
            Warning(message, md, md.DebugInformation.SequencePoints.FirstOrDefault());
        }

        private void AddMessage(string message, SequencePoint sequencePoint, DiagnosticMessage.Type diagnosticType)
        {
            _diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = diagnosticType,
                MessageData = message
            });
        }
    }
}
