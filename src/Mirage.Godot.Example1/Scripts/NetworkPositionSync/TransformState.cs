

using Godot;

namespace JamesFrowen.NetworkPositionSync;

/*
    public struct TransformState
    {
        public readonly Vector3 position;
        public readonly Vector3 rotation;

        public TransformState(Vector3 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public override string ToString()
        {
            return $"[{position}, {rotation}]";
        }

        public static ISnapshotInterpolator<TransformState> CreateInterpolator() => new Interpolator();

        private class Interpolator : ISnapshotInterpolator<TransformState>
        {
            public TransformState Lerp(TransformState a, TransformState b, float alpha)
            {
                var pos = a.position.Slerp(b.position, alpha);
                var rot = a.rotation.Slerp(b.rotation, alpha);
                //var pos = Vector3.Slerp(a.position, b.position, alpha);
                //var rot = Quaternion.Slerp(a.rotation, b.rotation, alpha);
                return new TransformState(pos, rot);
            }
        }
    }
*/


public readonly struct TransformState
{
    public readonly Vector2 Position;
    public readonly float Rotation;

    public TransformState(Vector2 position, float rotation)
    {
        this.Position = position;
        this.Rotation = rotation;
    }

    public override string ToString()
    {
        return $"[{Position}, {Rotation}]";
    }

    public static ISnapshotInterpolator<TransformState> CreateInterpolator() => new Interpolator();

    private class Interpolator : ISnapshotInterpolator<TransformState>
    {
        public TransformState Lerp(TransformState a, TransformState b, float alpha)
        {
            var pos = a.Position.Slerp(b.Position, alpha);
            var rot = Mathf.Lerp(a.Rotation, b.Rotation, alpha);
            //var pos = Vector3.Slerp(a.position, b.position, alpha);
            //var rot = Quaternion.Slerp(a.rotation, b.rotation, alpha);
            return new TransformState(pos, rot);
        }
    }
}
