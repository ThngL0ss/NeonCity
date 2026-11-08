using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// NPC trang trí đi lại trong thành phố: đi một đoạn, đứng nghỉ, đi tiếp, thỉnh thoảng quay đầu. Không có va chạm, không tấn công,
    /// không để ý tới người chơi (người, robot, thậm chí cả quái "vô hại" đều dùng script này).
    /// Hình ảnh nằm ở object con có SpriteAnimator với 2 clip: "walk" và "idle" (thiếu "idle" thì đứng yên ở khung đầu của "walk").
    /// Vùng đi lại: Min X / Max X (để trống = trong giới hạn của địa điểm).
    /// </summary>
    public class CityNpc : MonoBehaviour
    {
        [Tooltip("Tốc độ đi (đơn vị / giây). Mỗi lần đi lấy ngẫu nhiên trong khoảng 70–130% giá trị này")] public float speed = 1.5f;
        [Tooltip("Thời gian đi liên tục (giây) ít nhất / nhiều nhất")] public Vector2 walkTime = new Vector2(2f, 6f);
        [Tooltip("Thời gian đứng nghỉ (giây) ít nhất / nhiều nhất")] public Vector2 idleTime = new Vector2(0.8f, 3.5f);
        [Tooltip("Xác suất đứng nghỉ sau mỗi đoạn đi (1 = luôn dừng, 0 = đi liên tục)"), Range(0f, 1f)] public float restChance = 0.6f;
        [Tooltip("Vùng đi lại: Min X = Max X = 0 nghĩa là dùng giới hạn của địa điểm")] public float minX, maxX;
        [Tooltip("Tên clip đi / đứng trong SpriteAnimator")] public string walkClip = "walk", idleClip = "idle";
        [Tooltip("Hình gốc vẽ quay sang PHẢI; bật = quay sang TRÁI")] public bool artFacesLeft = false;

        SpriteAnimator anim;
        Transform vis;
        float dir = 1f, timer, curSpeed;
        bool walking;

        void Start()
        {
            anim = GetComponentInChildren<SpriteAnimator>();
            vis = anim != null ? anim.transform : transform;
            dir = Random.value < 0.5f ? -1f : 1f;
            timer = Random.Range(0f, 2f);
            walking = false;
            Choose();
        }

        float Lo { get { return maxX > minX ? minX : Cfg.LevelMinX + 3f; } }
        float Hi { get { return maxX > minX ? maxX : Cfg.LevelMaxX - 3f; } }

        void Choose()
        {
            walking = !walking;
            if (walking)
            {
                if (Random.value < 0.35f) dir = -dir;
                timer = Random.Range(walkTime.x, walkTime.y);
                curSpeed = speed * Random.Range(0.7f, 1.3f);
            }
            else
            {
                if (Random.value > restChance) { walking = true; timer = Random.Range(walkTime.x, walkTime.y); curSpeed = speed; return; }
                timer = Random.Range(idleTime.x, idleTime.y);
            }
        }

        void Update()
        {
            if (GameManager.State != GameState.Playing && GameManager.State != GameState.Cinematic) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) Choose();
            if (walking)
            {
                float x = transform.position.x + dir * curSpeed * Time.deltaTime;
                if (x < Lo) { x = Lo; dir = 1f; }
                else if (x > Hi) { x = Hi; dir = -1f; }
                var p = transform.position;
                p.x = x;
                transform.position = p;
            }
            if (vis != null)
            {
                var s = vis.localScale;
                float face = dir * (artFacesLeft ? -1f : 1f);
                s.x = Mathf.Abs(s.x) * (face >= 0f ? 1f : -1f);
                vis.localScale = s;
            }
            if (anim != null)
            {
                if (walking) anim.Play(walkClip);
                else anim.Play(anim.Has(idleClip) ? idleClip : walkClip);
            }
        }
    }
}
