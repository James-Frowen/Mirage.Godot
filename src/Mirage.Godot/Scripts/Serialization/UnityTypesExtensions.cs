using Godot;

namespace Mirage.Serialization
{
    public static class UnityTypesExtensions
    {
        public static void WriteVector2(this NetworkWriter writer, Vector2 value)
        {
            writer.WriteSingle(value.X);
            writer.WriteSingle(value.Y);
        }

        public static void WriteVector3(this NetworkWriter writer, Vector3 value)
        {
            writer.WriteSingle(value.X);
            writer.WriteSingle(value.Y);
            writer.WriteSingle(value.Z);
        }

        public static void WriteVector4(this NetworkWriter writer, Vector4 value)
        {
            writer.WriteSingle(value.X);
            writer.WriteSingle(value.Y);
            writer.WriteSingle(value.Z);
            writer.WriteSingle(value.W);
        }

        public static void WriteColor(this NetworkWriter writer, Color value)
        {
            writer.WriteSingle(value.R);
            writer.WriteSingle(value.G);
            writer.WriteSingle(value.B);
            writer.WriteSingle(value.A);
        }

        public static void WritePlane(this NetworkWriter writer, Plane value)
        {
            writer.WriteVector3(value.Normal);
            writer.WriteSingle(value.D);
        }

        public static Vector2 ReadVector2(this NetworkReader reader) => new Vector2(reader.ReadSingle(), reader.ReadSingle());
        public static Vector3 ReadVector3(this NetworkReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Vector4 ReadVector4(this NetworkReader reader) => new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Color ReadColor(this NetworkReader reader) => new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Plane ReadPlane(this NetworkReader reader) => new Plane(reader.ReadVector3(), reader.ReadSingle());
    }
}
