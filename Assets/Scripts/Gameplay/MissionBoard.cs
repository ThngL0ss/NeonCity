using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Hộp thư nhiệm vụ trong căn cứ. Ẩn cho tới khi người chơi nói chuyện xong với Albert lần đầu; khi đó nó "hiện ra" kèm hiệu ứng
    /// và thông báo chỉ đường. Đi tới, bấm F → mở bảng nhiệm vụ (MissionUi) để NHẬN nhiệm vụ. Nộp nguyên liệu / nhận thưởng thì nói chuyện với Albert.
    /// Nếu Scene Base chưa có hộp thư (chưa chạy menu Editor) thì nhiệm vụ được nhận tự động khi Albert giao.
    /// Lá cờ báo (child "Flag", tuỳ chọn) dựng lên khi có nhiệm vụ chưa nhận, hạ xuống khi đang làm.
    /// </summary>
    public class MissionBoard : MonoBehaviour, IInteractable
    {
        public static MissionBoard Inst;

        [Tooltip("Khoảng cách ngang tối đa để bấm F")] public float interactRange = 2.6f;
        public float interactHeight = 3f;
        [Tooltip("Vị trí nhãn gợi ý so với chân vật")] public Vector2 promptOffset = new Vector2(0f, 3.4f);
        [Tooltip("Lá cờ báo (tuỳ chọn)")] public Transform flag;
        [Tooltip("Màu vầng sáng / hiệu ứng")] public Color glowColor = new Color(0.1f, 0.95f, 1f, 1f);

        SpriteRenderer[] srs;
        float[] baseA;
        float appear = 1f;          // 0..1: độ hiện (0 = ẩn hẳn)
        bool appearing;

        public static bool Exists { get { return Inst != null; } }

        void Awake()
        {
            Inst = this;
            srs = GetComponentsInChildren<SpriteRenderer>(true);
            baseA = new float[srs.Length];
            for (int i = 0; i < srs.Length; i++) baseA[i] = srs[i].color.a;
            appear = BaseMissions.BoardUnlocked ? 1f : 0f;
            Apply();
        }

        void OnEnable() { Interactor.Register(this); }
        void OnDisable() { Interactor.Unregister(this); }
        void OnDestroy() { if (Inst == this) Inst = null; }

        void Apply()
        {
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                var c = srs[i].color;
                c.a = baseA[i] * appear;
                srs[i].color = c;
                srs[i].enabled = appear > 0.001f;
            }
        }

        void Update()
        {
            // vừa nói chuyện xong với Albert lần đầu: hộp thư hiện ra
            if (BaseMissions.BoardJustUnlocked && !BaseUi.IsOpen && !Cine.Active && GameManager.State == GameState.Playing)
            {
                BaseMissions.BoardJustUnlocked = false;
                appearing = true;
                appear = 0f;
                Sfx.Play("charge", 0.7f, 1.5f);
                Fx.Flare((Vector2)transform.position + new Vector2(0f, 1.6f), glowColor, 3.2f, 0.7f);
                Fx.Burst((Vector2)transform.position + new Vector2(0f, 1.6f), glowColor, 22, 5f, 0.7f, 0.12f);
                GameManager.Banner(Cfg.L("HỘP THƯ NHIỆM VỤ ĐÃ XUẤT HIỆN — ĐI TỚI ĐÓ, BẤM F", "MISSION MAILBOX APPEARED — WALK THERE, PRESS F"), Cfg.Cyan, 6f);
            }
            if (appearing)
            {
                appear = Mathf.MoveTowards(appear, 1f, Time.deltaTime / 0.8f);
                if (appear >= 1f) appearing = false;
                Apply();
            }
            else if (BaseMissions.BoardUnlocked && appear < 1f) { appear = 1f; Apply(); }

            if (flag != null)
            {
                bool up = BaseMissions.BoardUnlocked && BaseMissions.Current != null && !BaseMissions.Accepted;
                float target = up ? 0f : -75f;
                float z = flag.localEulerAngles.z; if (z > 180f) z -= 360f;
                float wig = up ? Mathf.Sin(Time.time * 5f) * 6f : 0f;
                flag.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(z, target + wig, 0.2f));
            }
        }

        // ---- IInteractable
        public bool CanInteract { get { return BaseMissions.BoardUnlocked && appear >= 1f && !BaseUi.IsOpen && GameManager.State == GameState.Playing && !GameManager.PlayerLocked; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + promptOffset; } }
        public string PromptText
        {
            get
            {
                if (BaseMissions.Current != null && !BaseMissions.Accepted) return Cfg.L("[F] NHẬN NHIỆM VỤ", "[F] TAKE A MISSION");
                return Cfg.L("[F] HỘP THƯ NHIỆM VỤ", "[F] MISSION MAILBOX");
            }
        }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, interactRange, interactHeight); }

        public void Interact()
        {
            BaseMissions.BoardOpenedOnce = true;
            BaseUi.Open(BaseMode.Missions);
            Sfx.Play("click", 0.6f, 1.4f);
        }
    }
}
