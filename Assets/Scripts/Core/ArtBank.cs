using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Kho tham chiếu các sprite dùng chung (hiệu ứng), material và prefab được sinh ra khi chơi.
    /// File asset: Assets/Resources/NeonBreaker/ArtBank.asset (do Build Project tạo).
    /// Muốn đổi hình hiệu ứng / prefab quái sinh ra: kéo thả vào các ô ở đây.
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Art Bank", fileName = "ArtBank")]
    public class ArtBank : ScriptableObject
    {
        [Header("Sprite hiệu ứng (mỗi sprite = 1 đơn vị thế giới)")]
        public Sprite square;
        public Sprite circle;
        public Sprite glow;
        public Sprite ring;
        public Sprite arc;
        public Sprite diamond;
        public Sprite triangle;
        public Sprite star;

        [Header("Material")]
        [Tooltip("Sprite thường")] public Material spriteMaterial;
        [Tooltip("Cộng sáng (phát sáng neon)")] public Material additiveMaterial;
        [Tooltip("Trắng toàn bộ - dùng cho hiệu ứng nháy khi trúng đòn")] public Material flashMaterial;

        [Header("Prefab sinh ra lúc chơi")]
        public GameObject walkerPrefab;
        public GameObject flyerPrefab;
        public GameObject bossPrefab;
        public GameObject shurikenPrefab;
        public GameObject boltPrefab;
        public GameObject wavePrefab;
        public GameObject pickupPrefab;

        public static ArtBank Load()
        {
            return Resources.Load<ArtBank>("NeonBreaker/ArtBank");
        }
    }
}
