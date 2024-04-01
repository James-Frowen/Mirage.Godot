using Godot;
using Mirage;

namespace Example2d
{
    [GlobalClass]
    public partial class MoveCube2d : NetworkBehaviour
    {
        [Export] private float moveRadius = 20;
        [Export] private float speed = 1;
        [Export] private Node2D root;

        private Vector2 target;

        public override void _Process(double delta)
        {
            if (!Identity.IsServer)
                return;

            // If we're close to the target, pick a new random target
            if (root.Position.DistanceTo(target) < 0.5f)
            {
                PickRandomTarget();
            }

            // Move towards the target
            var direction = (target - root.Position).Normalized();
            root.Position += direction * speed * (float)delta;
        }

        private void PickRandomTarget()
        {
            var randomX = (float)GD.RandRange(-moveRadius, moveRadius);
            var randomY = (float)GD.RandRange(-moveRadius, moveRadius);
            target = new Vector2(randomX, randomY).Normalized() * (float)GD.RandRange(0, moveRadius);
        }
    }
}

