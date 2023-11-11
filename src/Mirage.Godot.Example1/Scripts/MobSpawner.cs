using System;
using System.Collections.Generic;
using Godot;
using Mirage;

public partial class MobSpawner : Node
{
    [Export] private Path3D Path { get; set; }
    [Export] private PathFollow3D Location { get; set; }
    [Export] public PackedScene MobScene { get; set; }

    [Export] private NetworkManager _networkManager;
    private Random _random = new Random();
    private List<NetworkPlayer> _players = new();
    private int _prefabHash;

    public override void _EnterTree()
    {
        _prefabHash = PrefabHashHelper.GetPrefabHash(MobScene);
    }

    private bool TryGetRandomPlayer(out NetworkPlayer player)
    {
        _players.Clear();
        _players.AddRange(_networkManager.Server.Players);
        if (_players.Count != 0)
        {
            player = _players[_random.Next(_players.Count)];
            return true;
        }
        else
        {
            player = null;
            return false;
        }
    }

    public void OnMobTimerTimeout()
    {
        return;
        if (!TryGetRandomPlayer(out var player))
            return;
        var identity = player.Identity;
        var root = identity.Root;
        var target = (Node3D)root;

        var mob = MobScene.Instantiate<Mob>();
        // move to random point on path
        Location.ProgressRatio = GD.Randf();

        var playerPosition = target.Position;
        mob.Initialize(Location.Position, playerPosition);

        // Spawn the mob by adding it to the Main scene.
        // add to a child of this object
        AddChild(mob);

        var mobIdentity = mob.GetNetworkIdentity();
        mobIdentity.PrefabHash = _prefabHash;
        _networkManager.ServerObjectManager.Spawn(mobIdentity);
    }
}
