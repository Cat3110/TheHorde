using UnityEngine;

namespace Tests
{
    public class RandomMaterialSpawner : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");

        [Header("Grid")]
        public Mesh mesh;                 // назначь Cube
        public int countX = 25;
        public int countY = 40;           // 25*40 = 1000
        public float spacing = 1.5f;

        [Header("Materials")]
        [Range(1, 512)] public int uniqueMaterials = 120;
        public bool disableGPUInstancing = true;

        [Header("Templates (assign in Inspector)")]
        public Material litBaseTemplate;      // URP/Lit, без опций
        public Material litEmissionTemplate;  // URP/Lit, Emission ON
        public Material litAlphaClipTemplate; // URP/Lit, Alpha Clipping ON

        private Material[] _palette;

        void Awake()
        {
            if (mesh == null)
            {
                Debug.LogError("[RandomMaterialSpawner] Mesh не назначен.");
                enabled = false; return;
            }
            if (!litBaseTemplate || !litEmissionTemplate || !litAlphaClipTemplate)
            {
                Debug.LogError("[RandomMaterialSpawner] Назначь три шаблонных материала (Base/Emission/AlphaClip) в инспекторе.");
                enabled = false; return;
            }

            _palette = new Material[uniqueMaterials];
            for (int i = 0; i < uniqueMaterials; i++)
            {
                // Выбираем шаблон для этого индекса (≈ 1/3 на вариант)
                int block = Mathf.Max(1, uniqueMaterials / 3);
                Material template = (i < block) ? litBaseTemplate
                                   : (i < block * 2) ? litEmissionTemplate
                                   : litAlphaClipTemplate;

                var mat = new Material(template); // клон варианта, гарантирует наличие нужных keywords в билде
                if (disableGPUInstancing) mat.enableInstancing = false;

                // Разные цвета — чтобы получить множество уникальных материалов
                float hue = i / (float)uniqueMaterials;
                var color = Color.HSVToRGB(hue, 0.7f, 1f);

                mat.SetColor(BaseColor, color);

                if (template == litEmissionTemplate)
                {
                    mat.SetColor(EmissionColor, color * 1.2f);
                }
                if (template == litAlphaClipTemplate)
                {
                    mat.SetFloat(CutoffID, 0.5f);
                }

                _palette[i] = mat;
            }
        }

        void Start()
        {
            var parent = new GameObject("BatchTest").transform;
            var rnd = new System.Random(12345); // фиксированный сид для стабильности
            for (int y = 0; y < countY; y++)
            for (int x = 0; x < countX; x++)
            {
                var go = new GameObject($"i_{x}_{y}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(x * spacing, 0, y * spacing);

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                var mr = go.AddComponent<MeshRenderer>();
                // случайно распределяем материалы, чтобы чередования ломали естественную сортировку
                int idx = rnd.Next(_palette.Length);
                mr.sharedMaterial = _palette[idx];
            }
        }
    }
}