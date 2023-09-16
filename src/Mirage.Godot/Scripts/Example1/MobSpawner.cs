using Godot;

public partial class MobSpawner : Node
{
    [Export] private Path3D Path { get; set; }
    [Export] private PathFollow3D Location { get; set; }
    [Export] public PackedScene MobScene { get; set; }

    [Export] public Node3D Target { get; set; }

    public void OnMobTimerTimeout()
    {
        var mob = MobScene.Instantiate<Mob>();
        // move to random point on path
        Location.ProgressRatio = GD.Randf();

        var playerPosition = Target.Position;
        mob.Initialize(Location.Position, playerPosition);

        // Spawn the mob by adding it to the Main scene.
        // add to a child of this object
        AddChild(mob);
    }
}
