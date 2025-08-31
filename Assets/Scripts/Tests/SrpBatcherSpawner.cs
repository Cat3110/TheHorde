using UnityEngine;

namespace Tests
{
    public class SrpBatcherSpawner : MonoBehaviour
    {
        public Mesh mesh;               // назначь в инспекторе: Cube
        public Material material;       // назначь общий материал M_Test_SRPB
        public int countX = 25;
        public int countY = 40;         // 1000 объектов
        public float spacing = 1.5f;

        void Start()
        {
            var parent = new GameObject("BatchTest").transform;
            for (int y = 0; y < countY; y++)
            for (int x = 0; x < countX; x++)
            {
                var go = new GameObject($"i_{x}_{y}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(x * spacing, 0, y * spacing);
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material; // ВАЖНО: sharedMaterial один и тот же
            }
        }
    }
}