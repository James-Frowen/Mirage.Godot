using Godot;
using Mirage;

namespace Example1
{
    public partial class NetworkTransform3D : NetworkBehaviour
    {
        [Export] private Node3D _target;
        private Vector3 _previousPos;
        private Quaternion _previousRot;
        private Vector3 _targetPos;
        private Quaternion _targetRot;

        public override void _Process(double delta)
        {
            if (!Identity.IsSpawned)
                return;

            if (this.IsServer() || this.HasAuthority())
            {
                CheckChanged();
            }
            else
            {
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
            SendUpdate(pos, rot);
        }
        [ClientRpc]
        private void SendUpdate(Vector3 pos, Quaternion rot)
        {
            _targetPos = pos;
            _targetRot = rot;
        }

        private void MoveTowards()
        {
            _target.Position = _targetPos;
            _target.Quaternion = _targetRot;
        }
    }
}
