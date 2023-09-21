using System;
using System.Threading.Tasks;
using Mirage.Serialization;

namespace Mirage.RemoteCalls
{
    /// <summary>
    /// Methods used by weaver to send RPCs
    /// </summary>
    public static class ServerRpcSender
    {
        public static void Send(INetworkNode behaviour, int relativeIndex, NetworkWriter writer, Channel channelId, bool requireAuthority)
        {
            var index = behaviour.Identity.RemoteCallCollection.GetIndexOffset(behaviour) + relativeIndex;
            Validate(behaviour, index, requireAuthority);

            var message = new RpcMessage
            {
                NetId = behaviour.Identity.NetId,
                FunctionIndex = index,
                Payload = writer.ToArraySegment()
            };

            behaviour.Identity.Client.Player.Send(message, channelId);
        }

        public static Task SendWithReturn<T>(INetworkNode behaviour, int relativeIndex, NetworkWriter writer, bool requireAuthority)
        {
            var index = behaviour.Identity.RemoteCallCollection.GetIndexOffset(behaviour) + relativeIndex;
            Validate(behaviour, index, requireAuthority);
            var message = new RpcWithReplyMessage
            {
                NetId = behaviour.Identity.NetId,
                FunctionIndex = index,
                Payload = writer.ToArraySegment()
            };

            (var task, var id) = behaviour.Identity.ClientObjectManager._rpcHandler.CreateReplyTask<T>();

            message.ReplyId = id;

            // reply rpcs are always reliable
            behaviour.Identity.Client.Player.Send(message, Channel.Reliable);

            return task;
        }

        private static void Validate(INetworkNode behaviour, int index, bool requireAuthority)
        {
            var client = behaviour.Identity.Client;

            if (client == null || !client.Active)
            {
                var rpc = behaviour.Identity.RemoteCallCollection.GetRelative(behaviour, index);
                throw new InvalidOperationException($"ServerRpc Function {rpc} called on server without an active client.");
            }

            // if authority is required, then client must have authority to send
            if (requireAuthority && !behaviour.Identity.HasAuthority)
            {
                var rpc = behaviour.Identity.RemoteCallCollection.GetRelative(behaviour, index);
                throw new InvalidOperationException($"Trying to send ServerRpc for object without authority. {rpc}");
            }

            if (client.Player == null)
            {
                throw new InvalidOperationException("Send ServerRpc attempted with no client connection.");
            }
        }

        /// <summary>
        /// Used by weaver to check if ClientRPC should be invoked locally in host mode
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="target"></param>
        /// <param name="player">player used for RpcTarget.Player</param>
        /// <returns></returns>
        public static bool ShouldInvokeLocally(INetworkNode behaviour, bool requireAuthority)
        {
            // not client? error
            if (!behaviour.Identity.IsClient)
            {
                throw new InvalidOperationException("Server RPC can only be called when client is active");
            }

            // not host? never invoke locally
            if (!behaviour.Identity.IsServer)
                return false;

            // check if auth is required and that host has auth over the object
            if (requireAuthority && !behaviour.Identity.HasAuthority)
            {
                throw new InvalidOperationException($"Trying to send ServerRpc for object without authority.");
            }

            return true;
        }
    }
}

