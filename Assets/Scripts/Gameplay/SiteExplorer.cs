using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Đánh dấu "đã thám hiểm" một công trình KHÔNG vào được (DesTown4, Phòng thí nghiệm, Bãi rác, Khu mỏ bỏ hoang, ReWolrld):
    /// khi người chơi đi tới trước cửa (cách không quá <see cref="range"/> đơn vị) thì gọi Town.Explore(id) một lần rồi tự tắt.
    /// Toà nhà vào được thì tính khi bước vào (xem StreetBuildings.Place). Là con của toà nhà nên tự dừng khi toà nhà bị ẩn (người chơi đang ở trong nhà khác).
    /// </summary>
    public class SiteExplorer : MonoBehaviour
    {
        public string id;
        public float x, range = 8f;

        /// <summary>Gắn vào toà nhà; x = vị trí cửa (toạ độ thế giới).</summary>
        public static SiteExplorer Add(Transform building, string id, float x, float range = 8f)
        {
            var go = new GameObject("Explorer_" + id);
            go.transform.SetParent(building, false);
            var e = go.AddComponent<SiteExplorer>();
            e.id = id; e.x = x; e.range = range;
            if (Town.IsExplored(id)) e.enabled = false;
            return e;
        }

        void Update()
        {
            if (!GameManager.Active || Interior.Busy || BaseUi.IsOpen) return;
            var pl = Player.Instance;
            if (pl == null || pl.Dead) return;
            if (Mathf.Abs(pl.Position.x - x) > range) return;
            Town.Explore(id);
            enabled = false;
        }
    }
}
