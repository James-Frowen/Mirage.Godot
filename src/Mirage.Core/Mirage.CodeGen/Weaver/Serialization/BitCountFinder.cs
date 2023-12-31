using Mirage.CodeGen;
using Mirage.Serialization;
using Mono.Cecil;

namespace Mirage.Weaver.Serialization
{
    internal static class BitCountFinder
    {
        public static ValueSerializer GetSerializer(ICustomAttributeProvider attributeProvider, TypeReference fieldType)
        {
            var attribute = attributeProvider.GetCustomAttribute<BitCountAttribute>();
            if (attribute == null)
                return default;

            var bitCount = (int)attribute.ConstructorArguments[0].Value;

            if (bitCount <= 0)
                throw new BitCountException("BitCount should be above 0");

            var maxSize = BitPackHelper.GetTypeMaxSize(fieldType, "BitCount");

            if (bitCount > maxSize)
                throw new BitCountException($"BitCount can not be above target type size, bitCount:{bitCount}, max size:{maxSize}, type:{fieldType.Name}");

            return new BitCountSerializer(bitCount, BitPackHelper.GetConvertType(fieldType));
        }
    }
}
