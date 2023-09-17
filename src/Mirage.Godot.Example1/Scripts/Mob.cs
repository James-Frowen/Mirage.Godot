using Godot;

public partial class Mob : CharacterBody3D
{
    [Export] public int MinSpeed { get; set; } = 10;
    [Export] public int MaxSpeed { get; set; } = 18;

    public void Initialize(Vector3 startPosition, Vector3 playerPosition)
    {
        LookAtFromPosition(startPosition, playerPosition, Vector3.Up);
        RotateY((float)GD.RandRange(-Mathf.Pi / 4.0, Mathf.Pi / 4.0));

        var vel = Vector3.Forward * GD.RandRange(MinSpeed, MaxSpeed);
        // convert to local space
        Velocity = vel.Rotated(Vector3.Up, Rotation.Y);
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveAndSlide();
    }

    private void OnVisibilityNotifierScreenExited()
    {
        QueueFree();
    }
}
