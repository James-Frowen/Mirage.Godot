using System;
using Godot;
using Mirage;
using Mirage.Logging;

namespace Example1
{
    public partial class NetworkTransform2D : NetworkBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger<NetworkTransform3D>();

        [Export] private Node2D _target;
        private Vector2 _previousPos;
        private float _previousRot;
        private Vector2 _targetPos;
        private float _targetRot;

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
            var currentRot = _target.RotationDegrees;

            if (currentPos.DistanceTo(_previousPos) > 0.01f
                || (Math.Abs(_previousRot - currentRot) % 360) > 0.1f
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
        private void SendUpdateRelayed(Vector2 pos, float rot)
        {
            if (logger.LogEnabled()) logger.Log($"RPC ToServer: {Identity.NetId}, {pos} {rot}");
            _targetPos = pos;
            _targetRot = rot;
            SendUpdate(pos, rot);
        }
        [ClientRpc]
        private void SendUpdate(Vector2 pos, float rot)
        {
            if (logger.LogEnabled()) logger.Log($"RPC ToClient: {Identity.NetId}, {pos} {rot}");
            _targetPos = pos;
            _targetRot = rot;
        }

        private void MoveTowards()
        {
            if (logger.LogEnabled()) logger.Log($"MoveTowards: {Identity.NetId}, from[{_target.Position},{_target.RotationDegrees}] to[{_targetPos},{_targetRot}]");
            _target.Position = _targetPos;
            if (_targetRot != default) // dont set if rotation is all zeros
                _target.RotationDegrees = _targetRot;
        }
    }
}
