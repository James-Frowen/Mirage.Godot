using System;
using System.Text;
using Godot;
using Mirage;

namespace Mirage.Messages
{
    [NetworkMessage]
    public struct SpawnMessage
    {
        public uint NetId;
        public bool IsOwner;
        public int SpawnHash;
        public SpawnValues SpawnValues;
        public ArraySegment<byte> Payload;

        public override string ToString()
        {
            return $"SpawnMessage[NetId:{NetId},SpawnHash:{SpawnHash:X},IsOwner:{IsOwner},{SpawnValues},Payload:{Payload.Count}bytes]";
        }
    }

    public struct SpawnValues
    {
        public Vector3? Position;
        public Quaternion? Rotation;
        public Vector3? Scale;
        public string Name;
        public bool? SelfActive;

        [ThreadStatic]
        private static StringBuilder builder;

        public override string ToString()
        {
            if (builder == null)
                builder = new StringBuilder();
            else
                builder.Clear();

            builder.Append("SpawnValues(");
            var first = true;

            if (Position.HasValue)
                Append(ref first, $"Position={Position.Value}");

            if (Rotation.HasValue)
                Append(ref first, $"Rotation={Rotation.Value}");

            if (Scale.HasValue)
                Append(ref first, $"Scale={Scale.Value}");

            if (!string.IsNullOrEmpty(Name))
                Append(ref first, $"Name={Name}");

            if (SelfActive.HasValue)
                Append(ref first, $"SelfActive={SelfActive.Value}");

            builder.Append(")");
            return builder.ToString();
        }

        private static void Append(ref bool first, string value)
        {
            if (!first) builder.Append(", ");
            first = false;
            builder.Append(value);
        }
    }
}
