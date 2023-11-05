using Godot;

namespace Mirage.Serialization
{
    public static class GodotTypesExtensions
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

    public static class GodotCollectionExtensions
    {
        [WeaverSerializeCollection]
        public static void WriteGodotArray<[MustBeVariant] T>(this NetworkWriter writer, Godot.Collections.Array<T> array)
        {
            CollectionExtensions.WriteCountPlusOne(writer, array?.Count);

            if (array is null)
                return;

            var length = array.Count;
            for (var i = 0; i < length; i++)
                writer.Write(array[i]);
        }

        [WeaverSerializeCollection]
        public static void WriteGodotDictionary<[MustBeVariant] TKey, [MustBeVariant] TValue>(this NetworkWriter writer, Godot.Collections.Dictionary<TKey, TValue> dictionary)
        {
            CollectionExtensions.WriteCountPlusOne(writer, dictionary?.Count);

            if (dictionary is null)
                return;

            foreach (var kvp in dictionary)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }


        [WeaverSerializeCollection]
        public static Godot.Collections.Array<T> ReadGodotArray<[MustBeVariant] T>(this NetworkReader reader)
        {
            var hasValue = CollectionExtensions.ReadCountPlusOne(reader, out var length);
            if (!hasValue)
                return null;

            CollectionExtensions.ValidateSize(reader, length);

            var result = new Godot.Collections.Array<T>();
            result.Resize(length);
            for (var i = 0; i < length; i++)
            {
                result[i] = reader.Read<T>();
            }
            return result;
        }

        [WeaverSerializeCollection]
        public static Godot.Collections.Dictionary<TKey, TValue> ReadGodotDictionary<[MustBeVariant] TKey, [MustBeVariant] TValue>(this NetworkReader reader)
        {
            var hasValue = CollectionExtensions.ReadCountPlusOne(reader, out var length);
            if (!hasValue)
                return null;

            CollectionExtensions.ValidateSize(reader, length);

            var result = new Godot.Collections.Dictionary<TKey, TValue>();
            for (var i = 0; i < length; i++)
            {
                var key = reader.Read<TKey>();
                var value = reader.Read<TValue>();
                result[key] = value;
            }
            return result;
        }
    }
}
