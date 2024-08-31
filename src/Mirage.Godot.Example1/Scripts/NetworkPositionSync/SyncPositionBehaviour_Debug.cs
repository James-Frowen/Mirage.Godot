/*
namespace JamesFrowen.PositionSync
{
    public partial class SyncPositionBehaviour_Debug : NetworkBehaviour
    {
        private SyncPositionBehaviour behaviour;
        private List<Node> markers = new List<Node>();
        private SyncPositionSystem _system;
        public float maxTime = 5;
        public float MaxScale = 1;

        private void Awake()
        {
            behaviour = GetNode<SyncPositionBehaviour>("SyncPositionBehavior");
        }
        private void Update()
        {
            if (!Identity.IsClient) return;
            if (_system == null)
                _system = Identity.ClientObjectManager.GetNode<SyncPositionSystem>("SyncPositionSystem");

            foreach (var marker in markers)
                marker.SetActive(false);

            var buffer = behaviour.snapshotBuffer.DebugBuffer;
            for (var i = 0; i < buffer.Count; i++)
            {
                var snapshot = buffer[i];
                if (markers.Count <= i)
                    markers.Add(CreateMarker());

                markers[i].SetActive(true);
                markers[i].transform.SetPositionAndRotation(snapshot.state.position, snapshot.state.rotation);
                var pos = snapshot.state.position;
                var hash = (pos.x * 501) + pos.z;
                markers[i].GetComponent<Renderer>().material.color = Color.HSVToRGB(hash * 20 % 1, 1, 1);
                var snapshotTime = _system.TimeSync.Time;

                var absTimeDiff = Mathf.Abs((float)(snapshotTime - snapshot.time));
                var sizeFromDiff = Mathf.Clamp01((maxTime - absTimeDiff) / maxTime);
                var scale = sizeFromDiff * MaxScale;
                markers[i].transform.localScale = Vector3.one * scale;
            }
        }

        private Node CreateMarker()
        {
            var marker = Node3D.CreatePrimitive(PrimitiveType.Sphere);

            return marker;
        }
    }
}
*/
