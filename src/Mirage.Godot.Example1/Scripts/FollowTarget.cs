using Godot;

namespace Example1
{
    public partial class FollowTarget : Node
    {
        [Export] public Node3D Target;
        /// <summary>
        /// Smoothness multipled by delta time is how much is lerped each frame
        /// </summary>
        [Export] public float Speed = 10;

        private Node3D node;


        public override void _Ready()
        {
            node = GetParent<Node3D>();
        }

        public override void _Process(double delta)
        {
            if (Target is null)
                return;

            var lerp = (float)Mathf.Clamp(Speed * delta, 0, 1);
            node.Position = node.Position.Lerp(Target.Position, lerp);
        }
    }
}
