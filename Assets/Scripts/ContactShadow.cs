using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Subtle soft contact patch under the car. It SUPPLEMENTS the real directional
    /// shadow (it never replaces it), follows the actual road raycast and fades with
    /// suspension height so tyres do not look like they float.
    /// </summary>
    public sealed class ContactShadow : MonoBehaviour
    {
        public Transform Target;

        [SerializeField] private float size = 2.6f;
        [SerializeField] private float maxOpacity = 0.28f;
        [SerializeField] private float rayLength = 3.5f;

        private MeshRenderer _renderer;
        private Material _material;

        public static ContactShadow Create(Transform target)
        {
            GameObject go = new GameObject("Car Contact Shadow");
            ContactShadow shadow = go.AddComponent<ContactShadow>();
            shadow.Target = target;
            return shadow;
        }

        private void Start()
        {
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateQuad();

            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _material = new Material(shader) { name = "Soft Contact Shadow" };
                _material.mainTexture = CreateSoftTexture();
                _material.renderQueue = 2985;
            }
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (Target == null || _renderer == null) return;

            RaycastHit hit;
            Vector3 origin = Target.position + Vector3.up * 0.6f;
            if (!Physics.Raycast(origin, Vector3.down, out hit, rayLength,
                    ~0, QueryTriggerInteraction.Ignore) ||
                hit.collider == null || hit.collider.transform.root == Target.root ||
                hit.normal.y < 0.55f)
            {
                _renderer.enabled = false;
                return;
            }
            _renderer.enabled = true;

            float height = Mathf.Max(0f, Target.position.y - hit.point.y);
            float shrink = Mathf.Clamp(1f - height * 0.10f, 0.72f, 1.05f);
            transform.SetPositionAndRotation(hit.point + hit.normal * 0.025f,
                Quaternion.FromToRotation(Vector3.up, hit.normal));
            transform.localScale = new Vector3(size * shrink, 1f, size * 1.32f * shrink);

            if (_material != null)
            {
                Color c = _material.color;
                c.a = maxOpacity * Mathf.Clamp01(1f - height * 0.18f);
                _material.color = c;
            }
        }

        private static Mesh CreateQuad()
        {
            Mesh mesh = new Mesh { name = "Contact Shadow Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f)
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D CreateSoftTexture()
        {
            const int s = 128;
            Texture2D texture = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                name = "Soft Contact Shadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f;
                    float dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float alpha = Mathf.Pow(1f - d, 2.4f);
                    pixels[y * s + x] = new Color(0.02f, 0.02f, 0.025f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
