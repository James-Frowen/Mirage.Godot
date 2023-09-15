using System;

namespace Mirage
{
    /// <summary>
    /// Tell the weaver to generate  reader and writer for a class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class NetworkMessageAttribute : Attribute
    {
    }
}
