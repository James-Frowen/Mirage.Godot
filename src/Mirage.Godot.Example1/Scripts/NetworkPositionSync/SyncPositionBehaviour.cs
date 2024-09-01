using System;
using System.Runtime.CompilerServices;
using Godot;
using Mirage;
using Mirage.Logging;
using Mirage.Serialization;

namespace JamesFrowen.NetworkPositionSync;

/// <summary>
/// This NetworkBehaviour allows position and rotation synchronization over the network. Keep in mind that the <see cref="SyncPositionSystem"/> is 
/// still required in order to actually sync the data over the network.
/// </summary>
[GlobalClass]
public partial class SyncPositionBehaviour : NetworkBehaviour
{
    private static readonly ILogger logger = LogFactory.GetLogger<SyncPositionBehaviour>();

    /// <summary>
    /// Checks if object needs syncing to clients
    /// <para>Called on server</para>
    /// </summary>
    /// <returns>True if we need an update, false if no update is required.</returns>
    internal bool NeedsUpdate()
    {
        if (IsControlledByServer)
            return HasMoved() || HasRotated();
        else
            // If client authority, we don't care about the time the snapshot was sent, just parrot it to other clients.
            // todo do we need a check for attackers sending too many snapshots?
            return _needsUpdate;
    }

    /// <summary>
    /// Applies a state update on the server instance.
    /// <para>Called on server.</para>
    /// </summary>
    internal void ApplyOnServer(TransformState state, double time)
    {
        // This should not happen. throw an Exception to disconnect the attacker
        if (!_clientAuthority)
            throw new InvalidOperationException("Client is not allowed to send updated data when clientAuthority is false");

        // See comment in NeedsUpdate 
        _needsUpdate = true;
        _latestState = state;

        // If host then apply using interpolation, otherwise apply exact 
        if (Identity.IsClient)
            AddSnapShotToBuffer(state, time);
        else
            ApplyStateNoInterpolation(state);
    }

    /// <summary>
    /// Applies a state update on the client instance.
    /// <para>Called on client.</para>
    /// </summary>
    internal void ApplyOnClient(TransformState state, double time)
    {
        // Not host.
        // Host will have already handled movement in servers code
        if (Identity.IsServer)
            return;

        AddSnapShotToBuffer(state, time);
    }

    [ExportGroup("Synchronization Settings")]

    //[ExportSubgroup("What transform should be synchronized?")]
    [Export] private Node2D _target;

    //[ExportGroup("If true, we will use local position and rotation. If false, we use world position and rotation.")]
    [Export] private bool _useLocalSpace = true;

    [ExportSubgroup("Authority")]
    //[Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
    [Export] private bool _clientAuthority = false;

    //[Tooltip("Client Authority Sync Interval")]
    [Export] private float _clientSyncRate = 20;
    private float ClientFixedSyncInterval => 1 / _clientSyncRate;

    [ExportSubgroup("Debugging")]
    [Export] private bool showDebugGui = false;
    private float syncTimer;

    /// <summary>
    /// Set when client with authority updates the server
    /// </summary>
    private bool _needsUpdate;

    /// <summary>
    /// latest values from client
    /// </summary>
    private TransformState? _latestState;

    // values for HasMoved/Rotated
    [Export] private Vector2 _lastPosition;
    [Export] private float _lastRotation;

    // client
    internal readonly SnapshotBuffer<TransformState> _snapshotBuffer = new SnapshotBuffer<TransformState>(TransformState.CreateInterpolator());

#if DEBUG
    private ExponentialMovingAverage _timeDelayAvg = new ExponentialMovingAverage(100);

    private void OnGUI()
    {
        if (showDebugGui)
        {
            var delay = _system.TimeSync.LatestServerTime - _system.TimeSync.Time;
            _timeDelayAvg.Add(delay);
            //GUILayout.Label($"ServerTime: {_system.TimeSync.LatestServerTime:0.000}");
            //GUILayout.Label($"InterpTime: {_system.TimeSync.Time:0.000}");
            //GUILayout.Label($"Time Delta: {delay:0.000} smooth:{timeDelayAvg.Value:0.000} scale:{_system.TimeSync.DebugScale:0.000}");
            //GUILayout.Label(snapshotBuffer.ToDebugString(_system.TimeSync.Time));
        }
    }
#endif

    private void OnValidate()
    {
        _target ??= GetParent<Node2D>();
    }

    /// <summary>
    /// server auth or no owner, or host
    /// </summary>
    private bool IsControlledByServer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !_clientAuthority || Identity.Owner == null || Identity.Owner == Identity.Server.LocalPlayer;
    }

    /// <summary>
    /// Are we in control of this object when running a client auth setup and we own this object?
    /// </summary>
    private bool IsLocalClientInControl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _clientAuthority && Identity.HasAuthority;
    }

    [Export]
    private Vector2 Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _target.Position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _target.Position = value;
    }

    [Export]
    private float Rotation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _target.Rotation;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _target.Rotation = value;
    }

    public TransformState TransformState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // in client auth, we want to use the state given by the client.
        // that will be _latestState,
        get => _latestState ?? new TransformState(Position, Rotation);
    }

    /// <summary>
    /// Resets values, called after syncing to client
    /// <para>Called on server</para>
    /// </summary>
    internal void ClearNeedsUpdate()
    {
        _needsUpdate = false;
        _latestState = null;
        _lastPosition = Position;
        _lastRotation = Rotation;
    }

    /// <summary>
    /// Has target moved since we last checked
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasMoved()
    {
        var precision = _system.PackSettings.position_precision;
        var diff = _lastPosition - Position;
        var moved = Mathf.Abs(diff.X) > precision.X
                || Mathf.Abs(diff.Y) > precision.Y;

        if (moved)
        {
            _lastPosition = Position;
        }
        return moved;
    }

    /// <summary>
    /// Has target moved since we last checked
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasRotated()
    {
        // todo fix?
        var rotated = (_target.Rotation - Rotation) > 0.001f;

        if (rotated)
        {
            _lastRotation = Rotation;
        }
        return rotated;
    }

    public override void _Ready()
    {
        base._Ready();
        Identity.OnStartClient.AddListener(OnStartClient);
        Identity.OnStopClient.AddListener(OnStopClient);

        Identity.OnStartServer.AddListener(OnStartServer);
        Identity.OnStopServer.AddListener(OnStopServer);
        OnValidate();

    }

    private SyncPositionSystem _system;

    private void FindSystem()
    {
        if (Identity.IsServer)
        {
            _system = Identity.ServerObjectManager.GetParent().GetNode<SyncPositionSystem>("SyncPositionSystem");
            _system.Behaviours.AddBehaviour(this);
        }

        else if (Identity.IsClient)
        {
            _system = Identity.ClientObjectManager.GetParent().GetNode<SyncPositionSystem>("SyncPositionSystem");
            _system.Behaviours.AddBehaviour(this);
        }

        else throw new InvalidOperationException("System can't be found when object is not spawned");
    }


    #region Mirage Event Callbacks        
    public void OnStartClient()
    {
        // dont add twice in host mode
        if (Identity.IsServer) return;
        FindSystem();
    }

    public void OnStartServer()
    {
        FindSystem();
    }

    public void OnStopClient()
    {
        // dont add twice in host mode
        if (Identity.IsServer)
            return;

        // null check incase IsServer is set to false before OnStopClient is called
        _system?.Behaviours.RemoveBehaviour(this);
        _system = null;
    }

    public void OnStopServer()
    {
        // null check incase OnStopClient is called first
        _system?.Behaviours.RemoveBehaviour(this);
        _system = null;
    }
    #endregion

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Identity.IsClient)
        {
            if (IsLocalClientInControl)
            {
                ClientAuthorityUpdate();
            }

            else
            {
                ClientInterpolation();
            }

        }
    }

    #region Server Sync Update

    /// <summary>
    /// Applies values to target transform on client
    /// <para>Adds to buffer for interpolation</para>
    /// </summary>
    /// <param name="state"></param>
    private void AddSnapShotToBuffer(TransformState state, double serverTime)
    {
        // dont apply on local owner
        if (IsLocalClientInControl)
            return;

        // buffer will be empty if first snapshot or hasn't moved for a while.
        // in this case we can add a snapshot for (serverTime-syncinterval) for interoplation
        // this assumes snapshots are sent in order!
        if (_snapshotBuffer.IsEmpty)
        {
            // use new state here instead of TranformState incase update is from client auth when runing in host mode
            _snapshotBuffer.AddSnapShot(new TransformState(Position, Rotation), serverTime - ClientFixedSyncInterval);
        }
        _snapshotBuffer.AddSnapShot(state, serverTime);
    }
    #endregion


    #region Client Sync Update 
    private void ClientAuthorityUpdate()
    {
        // host client doesn't need to update server
        if (Identity.IsServer) { return; }

        // client can just use delta not (instead of unscaled)
        // this only job of this timer is to stop server being spammed with updates
        // when an update like this gets to the server it will just be updated right away,
        // the server can then send an update to the client on its next interval
        // todo, does server need to buffer updates for this?
        syncTimer += (float)GetProcessDeltaTime();
        if (syncTimer > ClientFixedSyncInterval)
        {
            syncTimer -= ClientFixedSyncInterval;
            if (HasMoved() || HasRotated())
            {
                SendMessageToServer();
                // todo move client auth uppdate to sync system
                ClearNeedsUpdate();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SendMessageToServer()
    {
        using (var writer = NetworkWriterPool.GetWriter())
        {
            // todo does client need to send time?
            //_system.packer.PackTime(writer, (float)NetworkTime.Time);
            _system.packer.PackNext(writer, this);

            var msg = new NetworkPositionSingleMessage
            {
                payload = writer.ToArraySegment()
            };
            Identity.Client.Send(msg, _system.MessageChannel);
        }
    }

    /// <summary>
    /// Applies values to target transform on server
    /// <para>no need to interpolate on server</para>
    /// </summary>
    /// <param name="state"></param>
    private void ApplyStateNoInterpolation(TransformState state)
    {
        Position = state.Position;
        Rotation = state.Rotation;
    }
    #endregion


    #region Client Interpolation
    private void ClientInterpolation()
    {
        if (_snapshotBuffer.IsEmpty)
            return;

        var snapshotTime = _system.TimeSync.Time;
        var state = _snapshotBuffer.GetLinearInterpolation(snapshotTime);
        // todo add trace log
        if (logger.LogEnabled())
            logger.Log($"p1: {Position.X}, p2: {state.Position.Y}, delta: {Position.X - state.Position.X}");

        Position = state.Position;
        Rotation = state.Rotation;
        GD.Print(state.Position, state.Rotation);

        // remove snapshots older than 2times sync interval, they will never be used by Interpolation
        var removeTime = snapshotTime - (_system.TimeSync.ClientDelay * 1.5f);
        _snapshotBuffer.RemoveOldSnapshots(removeTime);
    }
    #endregion

    #region Teleport
    public void Teleport(Vector2 position, float rotation)
    {
        _target.Position = position;
        _target.Rotation = rotation;
        RpcTeleport(position, rotation);
        GD.Print(position, rotation);
    }

    [ClientRpc]
    private void RpcTeleport(Vector2 position, float rotation)
    {
        _snapshotBuffer.ClearBuffer();
        _target.Position = position;
        _target.Rotation = rotation;
        GD.Print(position, rotation);
    }
    #endregion
}
