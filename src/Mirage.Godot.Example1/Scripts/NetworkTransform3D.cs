using Godot;
using Mirage;
using Mirage.Logging;

namespace Example1
{
    public partial class NetworkTransform3D : NetworkBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger<NetworkTransform3D>();

        [Export] private Node3D _target;
        private Vector3 _previousPos;
        private Quaternion _previousRot;
        private Vector3 _targetPos;
        private Quaternion _targetRot;

        public override void _Process(double delta)
        {
            if (!Identity.IsSpawned)
                return;

            if ((this.IsServer() && Identity.Owner == null) || this.HasAuthority())
            {
                if (logger.LogEnabled()) logger.Log($"CheckChanged: {Identity.NetId}");
                CheckChanged();
            }
            else
            {
                if (logger.LogEnabled()) logger.Log($"MoveTowards: {Identity.NetId}");
                MoveTowards();
            }
        }

        private void CheckChanged()
        {
            var currentPos = _target.Position;
            var currentRot = _target.Quaternion;

            if (currentPos.DistanceTo(_previousPos) > 0.01f
                || currentRot.AngleTo(_previousRot) > 0.01f
                )
            {
                if (this.IsServer())
                {
                    SendUpdate(currentPos, currentRot);
                }
                else
                {
                    SendUpdateRelayed(currentPos, currentRot);
                }
                _previousPos = currentPos;
                _previousRot = currentRot;
            }
        }

        [ServerRpc]
        private void SendUpdateRelayed(Vector3 pos, Quaternion rot)
        {
            if (logger.LogEnabled()) logger.Log($"RPC ToServer: {Identity.NetId}, {pos} {rot}");
            SendUpdate(pos, rot);
        }
        [ClientRpc]
        private void SendUpdate(Vector3 pos, Quaternion rot)
        {
            if (logger.LogEnabled()) logger.Log($"RPC ToClient: {Identity.NetId}, {pos} {rot}");
            _targetPos = pos;
            _targetRot = rot;
        }

        private void MoveTowards()
        {
            if (logger.LogEnabled()) logger.Log($"MoveTowards: {Identity.NetId}, from[{_target.Position},{_target.Quaternion}] to[{_targetPos},{_targetRot}]");
            _target.Position = _targetPos;
            _target.Quaternion = _targetRot;
        }
    }
}
