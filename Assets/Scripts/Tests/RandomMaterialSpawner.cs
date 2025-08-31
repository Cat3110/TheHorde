using UnityEngine;

namespace Tests
{
    public class RandomMaterialSpawner : MonoBehaviour
    {
        [Header("Grid")]
        public Mesh mesh;                 // назначь Cube
        public int countX = 25;
        public int countY = 40;           // 25*40 = 1000
        public float spacing = 1.5f;

        [Header("Materials")]
        public int uniqueMaterials = 120;  // 12 матов: 4 обычных, 4 с эмиссией, 4 с клипом
        public Shader shader;             // назначь "Universal Render Pipeline/Lit" или "Universal Render Pipeline/Unlit"
        public bool useLit = true;        // для Lit включим разные фичи (emission, alpha clip)
        public bool disableGPUInstancing = true;

        Material[] palette;

        void Awake()
        {
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
                useLit = true;
            }

            palette = new Material[uniqueMaterials];
            for (int i = 0; i < uniqueMaterials; i++)
            {
                var mat = new Material(shader);
                if (disableGPUInstancing) mat.enableInstancing = false;

                // Базовый цвет — различаем материалы
                var hue = (i / (float)uniqueMaterials);
                var color = Color.HSVToRGB(hue, 0.7f, 1f);

                if (useLit)
                {
                    // URP/Lit: базовый цвет
                    mat.SetColor("_BaseColor", color);

                    // Чередуем варианты шейдера (keywords), чтобы создать разные шейдер-варианты:
                    // 0..3: базовые; 4..7: Emission; 8..11: Alpha Clipping
                    if (i >= 4 && i < 8)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                        mat.SetColor("_EmissionColor", color * 1.2f);
                    }
                    else if (i >= 8)
                    {
                        mat.SetFloat("_AlphaClip", 1f);
                        mat.EnableKeyword("_ALPHATEST_ON");
                        // сделаем полупрозрачную базовую текстуру через цвет/альфу (для клипа нужен альфа-канал)
                        mat.SetFloat("_Surface", 0f); // Opaque, но с клипом
                        mat.SetFloat("_Cutoff", 0.5f);
                    }
                }
                else
                {
                    // URP/Unlit
                    mat.SetColor("_BaseColor", color);
                }

                palette[i] = mat;
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
                int idx = rnd.Next(palette.Length);
                mr.sharedMaterial = palette[idx];
            }
        }
    }
}