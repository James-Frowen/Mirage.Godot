using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mirage.Logging;
using Mirage.Serialization;

namespace Mirage.RemoteCalls
{
    public static class ClientRpcSender
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ClientRpcSender));

        public static void Send(INetworkNode behaviour, int relativeIndex, NetworkWriter writer, Channel channelId, bool excludeOwner)
        {
            var index = behaviour.Identity.RemoteCallCollection.GetIndexOffset(behaviour) + relativeIndex;
            Validate(behaviour, index);

            var message = CreateMessage(behaviour, index, writer);

            behaviour.Identity.Server.SendToObservers(behaviour.Identity, message, excludeLocalPlayer: true, excludeOwner, channelId: channelId);
        }

        public static void SendTarget(INetworkNode behaviour, int relativeIndex, NetworkWriter writer, Channel channelId, NetworkPlayer player)
        {
            var index = behaviour.Identity.RemoteCallCollection.GetIndexOffset(behaviour) + relativeIndex;
            Validate(behaviour, index);

            var message = CreateMessage(behaviour, index, writer);

            player = GetTarget(behaviour, player);

            player.Send(message, channelId);
        }

        public static Task<T> SendTargetWithReturn<T>(INetworkNode behaviour, int relativeIndex, NetworkWriter writer, NetworkPlayer player)
        {
            var index = behaviour.Identity.RemoteCallCollection.GetIndexOffset(behaviour) + relativeIndex;
            Validate(behaviour, index);

            (var task, var id) = behaviour.Identity.ServerObjectManager._rpcHandler.CreateReplyTask<T>();
            var message = new RpcWithReplyMessage
            {
                NetId = behaviour.Identity.NetId,
                FunctionIndex = index,
                ReplyId = id,
                Payload = writer.ToArraySegment()
            };

            player = GetTarget(behaviour, player);

            // reply rpcs are always reliable
            player.Send(message, Channel.Reliable);

            return task;
        }

        private static NetworkPlayer GetTarget(INetworkNode behaviour, NetworkPlayer player)
        {
            // player parameter is optional. use owner if null
            if (player == null)
                player = behaviour.Identity.Owner;

            // if still null throw to give useful error
            if (player == null)
                throw new InvalidOperationException("Player target was null for Rpc");

            return player;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RpcMessage CreateMessage(INetworkNode behaviour, int index, NetworkWriter writer)
        {
            var message = new RpcMessage
            {
                NetId = behaviour.Identity.NetId,
                FunctionIndex = index,
                Payload = writer.ToArraySegment()
            };
            return message;
        }

        private static void Validate(INetworkNode behaviour, int index)
        {
            var server = behaviour.Identity.Server;
            if (server == null || !server.Active)
            {
                var rpc = behaviour.Identity.RemoteCallCollection.GetRelative(behaviour, index);
                throw new InvalidOperationException($"RPC Function {rpc} called when server is not active.");
            }
        }

        /// <summary>
        /// Used by weaver to check if ClientRPC should be invoked locally in host mode
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="target"></param>
        /// <param name="player">player used for RpcTarget.Player</param>
        /// <returns></returns>
        public static bool ShouldInvokeLocally(INetworkNode behaviour, RpcTarget target, NetworkPlayer player)
        {
            // not server? error
            if (!behaviour.Identity.IsServer)
            {
                throw new InvalidOperationException("Client RPC can only be called when server is active");
            }

            // not host? never invoke locally
            if (!behaviour.Identity.IsClient)
                return false;

            // check if host player should receive
            switch (target)
            {
                case RpcTarget.Observers:
                    return IsLocalPlayerObserver(behaviour);
                case RpcTarget.Owner:
                    return IsLocalPlayerTarget(behaviour, behaviour.Identity.Owner);
                case RpcTarget.Player:
                    return IsLocalPlayerTarget(behaviour, player);
            }

            // should never get here
            throw new InvalidEnumArgumentException();
        }

        /// <summary>
        /// Checks if host player can see the object
        /// <para>Weaver uses this to check if RPC should be invoked locally</para>
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsLocalPlayerObserver(INetworkNode behaviour)
        {
            var local = behaviour.Identity.Server.LocalPlayer;
            return behaviour.Identity.observers.Contains(local);
        }

        /// <summary>
        /// Checks if host player is the target player
        /// <para>Weaver uses this to check if RPC should be invoked locally</para>
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsLocalPlayerTarget(INetworkNode behaviour, NetworkPlayer target)
        {
            var local = behaviour.Identity.Server.LocalPlayer;
            return local == target;
        }
    }
}

