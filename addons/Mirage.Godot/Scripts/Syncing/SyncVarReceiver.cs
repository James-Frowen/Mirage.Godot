using Mirage.Logging;
using Mirage.Messages;
using Mirage.Serialization;

namespace Mirage
{
    /// <summary>
    /// Class that handles syncvar message and passes it to correct <see cref="NetworkIdentity"/>
    /// </summary>
    public class SyncVarReceiver
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(SyncVarReceiver));

        private readonly IObjectLocator _objectLocator;

        /// <summary>
        /// skip messageHandler to not register UpdateVarsMessage
        /// </summary>
        /// <param name="objectLocator"></param>
        /// <param name="messageHandler"></param>
        public SyncVarReceiver(IObjectLocator objectLocator, MessageHandler messageHandler)
        {
            _objectLocator = objectLocator;
            if (messageHandler != null)
                messageHandler.RegisterHandler<UpdateVarsMessage>(OnUpdateVarsMessage);
        }


        private void OnUpdateVarsMessage(NetworkPlayer sender, UpdateVarsMessage msg)
        {
            if (logger.LogEnabled()) logger.Log("SyncVarReceiver.OnUpdateVarsMessage " + msg.NetId);

            if (_objectLocator.TryGetIdentity(msg.NetId, out var localObject))
            {
                // dont throw in Validate
                // owner or settings might have changed since client sent it
                if (!ValidateReceive(sender, localObject))
                    return;

                using (var networkReader = NetworkReaderPool.GetReader(msg.Payload, _objectLocator))
                    localObject.OnDeserializeAll(networkReader, false);
            }
            else
            {
                if (logger.WarnEnabled()) logger.LogWarning("Did not find target for sync message for " + msg.NetId + " . Note: this can be completely normal because UDP messages may arrive out of order, so this message might have arrived after a Destroy message.");
            }
        }

        private bool ValidateReceive(NetworkPlayer sender, NetworkIdentity identity)
        {
            // only need to validate if we are server
            // client can always receive from server
            if (!identity.IsServer)
                return true;

            // only owner of object can send to server
            if (identity.Owner != sender)
            {
                if (logger.WarnEnabled()) logger.LogWarning($"UpdateVarsMessage for object without authority [netId={identity.NetId}]");
                return false;
            }

            var behaviours = identity.NetworkBehaviours;
            for (var i = 0; i < behaviours.Length; i++)
            {
                var comp = behaviours[i];
                // check if any sync setting have to.server 
                // if we find atleast 1, then that is enough to start reading
                // we check each component again when we read it

                if (comp is INetworkNodeWithSettings withSettings)
                {
                    // we dont need to check From.Owner, if we are sending to server we must be sending from owner
                    if ((withSettings.SyncSettings.To & SyncTo.Server) != 0)
                        return true;
                }
            }

            if (logger.WarnEnabled()) logger.LogWarning($"UpdateVarsMessage for object without any NetworkBehaviours with SyncFrom.Owner [netId={identity.NetId}]");
            return false;
        }
    }
}
