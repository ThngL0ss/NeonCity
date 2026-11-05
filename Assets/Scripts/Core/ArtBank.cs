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

        [Header("Tiền & kho đồ (menu Tools ▸ Neon Breaker ▸ Thêm bẫy + kho đồ + rương tự gán)")]
        [Tooltip("Prefab đồng xu rơi ra khi hạ quái. Để trống = xu được cộng thẳng vào ví")] public GameObject coinPrefab;
        [Tooltip("Hình đồng xu vẽ trên HUD")] public Sprite coinIcon;

        [Header("Skill & minibot (menu Tools ▸ Neon Breaker ▸ Thêm hệ thống Skill tự gán)")]
        [Tooltip("Prefab linh kiện rơi ra khi hạ quái. Để trống = linh kiện được cộng thẳng vào kho")] public GameObject partPrefab;
        [Tooltip("Hình linh kiện vẽ trên HUD / giao diện Skill")] public Sprite partIcon;

        [Header("Dầu & vật phẩm rải rác (menu Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ tự gán)")]
        [Tooltip("Prefab giọt dầu rơi ra khi hạ quái. Để trống = dầu được cộng thẳng")] public GameObject oilPrefab;
        [Tooltip("Hình dầu vẽ trên HUD / giao diện")] public Sprite oilIcon;
        [Tooltip("Prefab vật phẩm nằm trên đường, bấm F để nhặt (thùng dầu, nguyên liệu)")] public GameObject worldItemPrefab;

        public static ArtBank Load()
        {
            return Resources.Load<ArtBank>("NeonBreaker/ArtBank");
        }
    }
}
