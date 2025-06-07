using Mirage.SocketLayer;

namespace Mirage
{
    public class MetricSettings
    {
        public readonly bool Enabled;
        /// <summary>
        /// Sequence size of buffer in bits.\n10 => array size 1024 => ~17 seconds at 60hz
        /// </summary>
        public readonly int Size;

        /// <summary>
        /// Set by MiragePeer when SocketLayer is enabled
        /// </summary>
        public Metrics Metrics;
    }
}
