using System;
using Mirage.Events;
using Mirage.Logging;
using Mirage.Serialization;
using Mirage.SocketLayer;

namespace Mirage
{
    public abstract class MiragePeer
    {
        public MetricSettings MetricsSettings { get; private set; }
        /// <summary>
        /// Config for peer, if not set will use default settings
        /// </summary>
        public Config PeerConfig { get; private set; }

        /// <summary>
        /// Should connection be disconnect if there is an exception inside a message handler
        /// </summary>
        public bool DisconnectOnException { get; private set; } = true;
        /// <summary>
        /// Should the message handler rethrow the exception after logging. This should only be used when deubgging as it may stop other Mirage functions from running after messages handling
        /// </summary>
        public bool RethrowException { get; private set; } = false;

        public Peer Peer { get; protected set; }
        public NetworkTime Time { get; protected set; }
        public MessageHandler MessageHandler { get; protected set; }

        public MiragePeer()
        {
            Setup();
        }
        public void Setup(bool disconnectOnException = true, bool rethrowException = false, Config peerConfig = null, MetricSettings metric = null)
        {
            DisconnectOnException = disconnectOnException;
            RethrowException = rethrowException;
            PeerConfig = peerConfig ?? new Config();
            MetricsSettings = metric ?? new MetricSettings();
        }


        protected readonly AddLateEvent _started = new AddLateEvent();
        protected readonly AddLateEvent _stopped = new AddLateEvent();

        public IAddLateEvent Started => _started;
        public IAddLateEvent Stopped => _stopped;

        public bool Active { get; protected set; }

        protected void Start(ISocket socket, int maxPacketSize, Func<IMessageReceiver, IDataHandler> createDataHandler)
        {
            ThrowIfActive();
            Active = true;

            MessageHandler = new MessageHandler(null, DisconnectOnException, RethrowException);
            var dataHandler = createDataHandler.Invoke(MessageHandler);
            //var dataHandler = new DataHandler(MessageHandler);
            MetricsSettings.Metrics = MetricsSettings.Enabled ? new Metrics(MetricsSettings.Size) : null;

            Time = new NetworkTime();

            var config = PeerConfig ?? new Config();
            NetworkWriterPool.Configure(maxPacketSize);

            Peer = new Peer(socket, maxPacketSize, dataHandler, config, LogFactory.GetLogger<Peer>(), MetricsSettings.Metrics);
            AddPeerEvents();
        }

        private void ThrowIfActive()
        {
            if (Active) throw new InvalidOperationException("Client is already active");
        }

        protected virtual void Cleanup()
        {
            if (Active)
                _stopped.Invoke();

            Active = false;

            if (Peer != null)
            {
                //remove handlers first to stop loop
                RemovePeerEvents();
                Peer.Close();
                Peer = null;
            }
        }

        protected abstract void AddPeerEvents();
        protected abstract void RemovePeerEvents();

        public virtual void UpdateReceive() => Peer?.UpdateReceive();
        public virtual void UpdateSent() => Peer.UpdateSent();
    }
}
