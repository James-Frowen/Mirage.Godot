using System;

namespace Mirage.Messages
{
    [NetworkMessage]
    public struct UpdateVarsMessage
    {
        public uint NetId;
        public ArraySegment<byte> Payload;
    }

    [NetworkMessage]
    public struct RemoveAuthorityMessage
    {
        public uint NetId;
    }

    [NetworkMessage]
    public struct RemoveCharacterMessage
    {
        public bool KeepAuthority;
    }

    [NetworkMessage]
    public struct ObjectDestroyMessage
    {
        public uint NetId;
    }

    [NetworkMessage]
    public struct ObjectHideMessage
    {
        public uint NetId;
    }
}
