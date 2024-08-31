using System;
using Godot;
using Mirage;
using Mirage.Serialization;
using static JamesFrowen.NetworkPositionSync.SyncPacker;

namespace JamesFrowen.NetworkPositionSync
{
    [Serializable]
    public class SyncSettings
    {
        [ExportCategory("Object Settings")]
        //[Tooltip("Required if using multiple SyncPositionBehaviour per NetworkIdentity, but increase bandwidth")]
        [Export] public bool IncludeComponentIndex;

        [ExportCategory("Timer Compression")]
        // public float maxTime = 60 * 60 * 24;
        // 0.1ms
        [Export] public float timePrecision = 1 / 10_000f;

        [ExportGroup("Var Size Compression")]
        //[Tooltip("How many bits will be used for a value before having to include another block.\nBest value will be a fraction of log2(worldSize / precision).\n" +
        //"The default values of 5 Block Size and 1/1000 precision will mean values under 54m will be 18 bits, values under 1747m will be 24 bits")]

        [Export] public int blockSize = 5;

        [ExportGroup("Position Compression")]
        //public Vector3 max = Vector3.one * 100;
        [Export] float precision = 300f;
        [Export] public Vector2 position_precision = Vector2.One / 300f;

        [ExportGroup("Rotation Compression")]
        //public bool syncRotation = true;
        [Export] public int bitCount = 10;

        // Data Packers.
        public VarDoublePacker CreateTimePacker()
        {
            return new VarDoublePacker(timePrecision, blockSize);
        }
        public VarVector2Packer CreatePositionPacker()
        {
            return new VarVector2Packer(position_precision, blockSize);
        }
        public VarFloatPacker CreateRotationPacker()
        {
            return new VarFloatPacker(precision, blockSize);
        }
    }

    public class SyncPacker
    {
        // packers
        private readonly VarDoublePacker _timePacker;
        private readonly VarVector2Packer _positionPacker;
        private readonly VarFloatPacker _rotationPacker;
        private readonly int _blockSize;
        private readonly bool _includeCompId;

        public SyncPacker(SyncSettings settings)
        {
            _timePacker = settings.CreateTimePacker();
            _positionPacker = settings.CreatePositionPacker();
            _rotationPacker = settings.CreateRotationPacker();
            _blockSize = settings.blockSize;
            _includeCompId = settings.IncludeComponentIndex;
        }

        public void PackTime(NetworkWriter writer, double time)
        {
            _timePacker.Pack(writer, time);
        }

        public void PackNext(NetworkWriter writer, SyncPositionBehaviour behaviour)
        {
            var id = behaviour.Identity;
            var state = behaviour.TransformState;


            VarIntBlocksPacker.Pack(writer, id.NetId, _blockSize);

            if (_includeCompId)
                VarIntBlocksPacker.Pack(writer, (uint)behaviour.ComponentIndex, _blockSize);

            _positionPacker.Pack(writer, state.Position);
            _rotationPacker.Pack(writer, state.Rotation);
        }


        public double UnpackTime(NetworkReader reader)
        {
            return _timePacker.Unpack(reader);
        }

        public void UnpackNext(NetworkReader reader, out NetworkBehaviour.Id id, out Vector2 pos, out float rot)
        {
            var netId = (uint)VarIntBlocksPacker.Unpack(reader, _blockSize);
            if (_includeCompId)
            {
                var componentIndex = (int)VarIntBlocksPacker.Unpack(reader, _blockSize);
                id = new NetworkBehaviour.Id(netId, componentIndex);
            }
            else
            {
                id = new NetworkBehaviour.Id(netId, 0);
            }

            pos = _positionPacker.Unpack(reader);
            rot = _rotationPacker.Unpack(reader);
        }

        internal bool TryUnpackNext(PooledNetworkReader reader, out NetworkBehaviour.Id id, out Vector2 pos, out float rot)
        {
            // assume 1 state is atleast 3 bytes
            // (it should be more, but there shouldn't be random left over bits in reader so 3 is enough for check)
            const int minSize = 3;
            if (reader.CanReadBytes(minSize))
            {
                UnpackNext(reader, out id, out pos, out rot);
                return true;
            }
            else
            {
                id = default;
                pos = default;
                rot = default;
                return false;
            }
        }



        /// <summary>
        /// Packs a float using <see cref="ZigZag"/> and <see cref="VarIntBlocksPacker"/>
        /// </summary>
        public sealed class VarDoublePacker
        {
            private readonly int _blockSize;
            private readonly double _precision;
            private readonly double _inversePrecision;

            public VarDoublePacker(double precision, int blockSize)
            {
                _precision = precision;
                _blockSize = blockSize;
                _inversePrecision = 1 / precision;
            }

            public void Pack(NetworkWriter writer, double value)
            {
                var scaled = (long)Math.Round(value * _inversePrecision);
                var zig = ZigZag.Encode(scaled);
                VarIntBlocksPacker.Pack(writer, zig, _blockSize);
            }

            public double Unpack(NetworkReader reader)
            {
                var zig = VarIntBlocksPacker.Unpack(reader, _blockSize);
                var scaled = ZigZag.Decode(zig);
                return scaled * _precision;
            }
        }
    }

    //[Serializable]
    //public class SyncSettingsDebug
    //{
    //    // todo replace these serialized fields with custom editor
    //    public bool drawGizmo;
    //    public Color gizmoColor;
    //    [Tooltip("readonly")]
    //    public int _posBitCount;
    //    [Tooltip("readonly")]
    //    public Vector3Int _posBitCountAxis;
    //    [Tooltip("readonly")]
    //    public int _posByteCount;

    //    public int _totalBitCountMin;
    //    public int _totalBitCountMax;
    //    public int _totalByteCountMin;
    //    public int _totalByteCountMax;

    //    internal void SetValues(SyncSettings settings)
    //    {
    //        var positionPacker = new Vector3Packer(settings.max, settings.precision);
    //        _posBitCount = positionPacker.bitCount;
    //        _posBitCountAxis = positionPacker.BitCountAxis;
    //        _posByteCount = Mathf.CeilToInt(_posBitCount / 8f);

    //        var timePacker = new FloatPacker(0, settings.maxTime, settings.timePrecision);
    //        var idPacker = new UIntVariablePacker(settings.smallBitCount, settings.mediumBitCount, settings.largeBitCount);
    //        UIntVariablePacker parentPacker = idPacker;
    //        var rotationPacker = new QuaternionPacker(settings.bitCount);


    //        _totalBitCountMin = idPacker.minBitCount + (settings.syncRotation ? rotationPacker.bitCount : 0) + positionPacker.bitCount;
    //        _totalBitCountMax = idPacker.maxBitCount + (settings.syncRotation ? rotationPacker.bitCount : 0) + positionPacker.bitCount;
    //        _totalByteCountMin = Mathf.CeilToInt(_totalBitCountMin / 8f);
    //        _totalByteCountMax = Mathf.CeilToInt(_totalBitCountMax / 8f);
    //    }
    //}
}
