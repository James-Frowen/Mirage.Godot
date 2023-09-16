using Godot;

namespace Example1
{
    public partial class Player : CharacterBody3D
    {
        [Export]
        public int Speed { get; set; } = 14;
        [Export]
        public int FallAcceleration { get; set; } = 75;

        private Vector3 _targetVelocity = Vector3.Zero;
        private Node3D _pivot;

        public override void _Ready()
        {
            _pivot = GetNode<Node3D>("Pivot");
        }

        public override void _PhysicsProcess(double delta)
        {
            var direction = GetInput();
            if (direction != Vector3.Zero)
            {
                direction = direction.Normalized();
                _pivot.LookAt(Position + direction, Vector3.Up);
            }

            _targetVelocity.X = direction.X * Speed;
            _targetVelocity.Z = direction.Z * Speed;

            if (!IsOnFloor())
            {
                _targetVelocity.Y -= FallAcceleration * (float)delta;
            }

            // set values on CharacterBody3D to move
            Velocity = _targetVelocity;
            MoveAndSlide();
        }

        private static Vector3 GetInput()
        {
            var direction = Vector3.Zero;

            if (Input.IsActionPressed("move_right"))
            {
                direction.X += 1.0f;
            }
            if (Input.IsActionPressed("move_left"))
            {
                direction.X -= 1.0f;
            }
            if (Input.IsActionPressed("move_back"))
            {
                direction.Z += 1.0f;
            }
            if (Input.IsActionPressed("move_forward"))
            {
                direction.Z -= 1.0f;
            }

            return direction;
        }
    }
}
