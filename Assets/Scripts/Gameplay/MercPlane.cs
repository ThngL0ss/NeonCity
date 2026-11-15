using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Máy bay thả bom của Connie: bay ngang màn hình (y cao), thả bom đều đặn khi ở trong dải [fromX, toX], rồi bay ra khỏi màn hình.</summary>
    public class MercPlane : MonoBehaviour
    {
        int dir;
        float fromX, toX, bombDmg, bombRadius, dropT, life = 6f;
        const float Speed = 15f;
        static Sprite sprite;

        public static void Spawn(float x, int dir, float y, float fromX, float toX, float bombDmg, float bombRadius)
        {
            Art.Init();
            var go = new GameObject("MercPlane");
            go.transform.position = new Vector3(x, y, 0f);
            var p = go.AddComponent<MercPlane>();
            p.dir = dir; p.fromX = fromX; p.toX = toX; p.bombDmg = bombDmg; p.bombRadius = bombRadius;
            var sr = new GameObject("Visual").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(go.transform, false);
            sr.sharedMaterial = Art.DefMat;
            sr.sprite = PlaneSprite();
            sr.sortingOrder = 50;
            sr.transform.localScale = new Vector3(dir, 1f, 1f);
            Sfx.PlayAt("slam", go.transform.position, 0.5f, 0.5f);
        }

        static Sprite PlaneSprite()
        {
            if (sprite != null) return sprite;
            var c = new PxCanvas(40, 14);
            var body = PxCanvas.C(0x7A8296); var dark = PxCanvas.C(0x4A5062); var light = PxCanvas.C(0xB8C0D4); var glass = PxCanvas.C(0x35E8FF);
            c.Rect(4, 5, 30, 4, body);                  // thân
            c.Rect(34, 6, 4, 2, dark);                  // mũi
            c.Rect(36, 5, 2, 1, light);
            c.Rect(14, 1, 6, 5, dark);                  // cánh trên
            c.Rect(12, 9, 8, 3, dark);                  // cánh dưới
            c.Rect(2, 2, 3, 4, dark);                   // đuôi
            c.Rect(26, 3, 5, 2, glass);                 // kính lái
            c.Rect(4, 5, 30, 1, light);
            c.Rect(9, 6, 2, 2, PxCanvas.C(0xFF9A1A));
            c.Outline(PxCanvas.C(0x0A0A16));
            sprite = c.Bake(0.5f, 0.5f);
            return sprite;
        }

        void Update()
        {
            if (BaseUi.IsOpen || !GameManager.Active) return;
            float dt = Time.deltaTime;
            transform.position += new Vector3(dir * Speed * dt, 0f, 0f);
            life -= dt;
            float x = transform.position.x;
            dropT -= dt;
            if (dropT <= 0f && x >= fromX && x <= toX)
            {
                dropT = 0.38f;
                MercBomb.Drop(transform.position + new Vector3(0f, -0.4f, 0f), bombDmg, bombRadius);
            }
            if (life <= 0f || (dir > 0 && x > toX + 8f) || (dir < 0 && x < fromX - 8f)) Destroy(gameObject);
        }
    }
}
