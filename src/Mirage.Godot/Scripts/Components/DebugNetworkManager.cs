using System;
using System.IO;
using Godot;
using Mirage.Logging;
using Mirage.SocketLayer;

namespace Mirage
{
    public partial class DebugNetworkManager : NetworkManager
    {
        public override void _Ready()
        {
            base._Ready();
            SetupLgging();

            if (EnableAllLogs)
            {
                LogFactory.SetDefaultLogLevel(LogType.Log, true);
                LogFactory.GetLogger<Peer>().filterLogType = LogType.Warning;
                LogFactory.GetLogger("Example1.NetworkTransform3D").filterLogType = LogType.Warning;
            }

            //LogFactory.GetLogger<SyncVarSender>().filterLogType = LogType.Log;
            //LogFactory.GetLogger<SyncVarReceiver>().filterLogType = LogType.Log;
            LogFactory.GetLogger<NetworkManager>().filterLogType = LogType.Log;
        }

        private static void SetupLgging()
        {
            var file = $"./MirageLogs/mirage_{DateTime.Now:HH.mm.ss}_{System.Environment.ProcessId}.log";
            GD.Print($"Creating log file {Path.GetFullPath(file)}");
            var fileLogger = new FileLogger(file, true, true);
            var godotLogger = new GodotLogger();
            LogFactory.ReplaceLogHandler(new MultiLogger(godotLogger, fileLogger), true);
        }

        private Config GetConfig()
        {
            return new Config
            {
                TimeoutDuration = 600, // really high for debugging
                MaxConnections = MaxConnections,
            };
        }

        public override void StartServer()
        {
            Server.PeerConfig = GetConfig();
            base.StartServer();
        }

        public override void StartClient()
        {
            Client.PeerConfig = GetConfig();
            base.StartClient();
        }

        public override void StartHost()
        {
            Server.PeerConfig = GetConfig();
            base.StartHost();
        }
    }
}
