using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// NPC trong toà nhà (người vẽ bằng code, xem TownArt). Ba kiểu: Stand (đứng yên, đúng chỗ), Wander (đi lại trong khoảng minX–maxX, thỉnh thoảng đứng nghỉ),
    /// Sit (ngồi thiền). Có onTalk thì bấm F nói chuyện được; không có thì chỉ là người trang trí. Khi có bảng giao diện mở, NPC đứng yên.
    /// </summary>
    public class TownNpc : MonoBehaviour, IInteractable
    {
        public enum Mode { Stand, Wander, Sit }

        public Mode mode = Mode.Stand;
        public string npcName = "";
        public System.Action onTalk;
        public float minX, maxX, speed = 1.3f, range = 2.2f;

        Sprite idle, walkA, walkB, sit;
        SpriteRenderer sr;
        float dir = 1f, timer, animT;
        bool walking;
        bool registered;

        /// <summary>Tạo NPC dưới parent tại x thế giới; khoảng đi lại [minWorldX, maxWorldX] (âm = lệch 4 đơn vị hai bên).</summary>
        public static TownNpc Spawn(Transform parent, Look look, float worldX, Mode mode, string name, System.Action talk, float minWorldX, float maxWorldX, bool faceLeft)
        {
            Art.Init();
            var go = new GameObject("Npc_" + look.id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldX, 0f, 0f);
            var n = go.AddComponent<TownNpc>();
            n.mode = mode;
            n.npcName = name ?? "";
            n.onTalk = talk;
            n.dir = faceLeft ? -1f : 1f;
            n.minX = minWorldX > -1e8f ? minWorldX : worldX - 4f;
            n.maxX = maxWorldX > -1e8f ? maxWorldX : worldX + 4f;
            n.speed = 1.0f + Random.value * 0.7f;
            if (mode == Mode.Sit) n.sit = TownArt.Sit(look);
            else { n.idle = TownArt.Human(look, 0); n.walkA = TownArt.Human(look, 1); n.walkB = TownArt.Human(look, 2); }
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            n.sr = vis.AddComponent<SpriteRenderer>();
            n.sr.sharedMaterial = Art.DefMat;
            n.sr.sortingOrder = 14;
            n.sr.sprite = mode == Mode.Sit ? n.sit : n.idle;
            n.timer = Random.Range(0.2f, 2.5f);
            n.Face();
            n.Refresh();
            return n;
        }

        public bool CanInteract { get { return onTalk != null && !Interior.Busy && !BaseUi.IsOpen && GameManager.State == GameState.Playing && !GameManager.PlayerLocked; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + new Vector2(0f, mode == Mode.Sit ? 2.0f : 2.6f); } }
        public string PromptText { get { return "[F] " + Cfg.L("NÓI CHUYỆN", "TALK") + (npcName.Length > 0 ? " — " + npcName : ""); } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, range, 3f); }

        public void Interact()
        {
            var pl = Player.Instance;
            if (pl != null && mode != Mode.Sit) { dir = pl.Position.x >= transform.position.x ? 1f : -1f; Face(); }
            walking = false;
            Refresh();
            if (onTalk != null) onTalk();
        }

        /// <summary>Có thể gán / đổi lời thoại sau khi tạo.</summary>
        public void SetTalk(System.Action talk) { onTalk = talk; Refresh(); }

        void Refresh()
        {
            bool want = onTalk != null;
            if (want && !registered && isActiveAndEnabled) { Interactor.Register(this); registered = true; }
            else if (!want && registered) { Interactor.Unregister(this); registered = false; }
        }

        void OnEnable() { registered = false; Refresh(); }
        void OnDisable() { if (registered) Interactor.Unregister(this); registered = false; }

        void Face()
        {
            if (sr == null) return;
            var s = sr.transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir >= 0f ? 1f : -1f);
            sr.transform.localScale = s;
        }

        void Update()
        {
            if (mode != Mode.Wander) return;
            if (BaseUi.IsOpen || Interior.Busy || GameManager.State != GameState.Playing) return;
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                walking = !walking;
                if (walking)
                {
                    timer = Random.Range(1.5f, 4.5f);
                    if (Random.value < 0.4f) dir = -dir;
                }
                else timer = Random.Range(0.8f, 3f);
                Face();
            }
            if (walking)
            {
                float x = transform.position.x + dir * speed * Time.deltaTime;
                if (x < minX) { x = minX; dir = 1f; Face(); }
                else if (x > maxX) { x = maxX; dir = -1f; Face(); }
                var p = transform.position; p.x = x; transform.position = p;
                animT += Time.deltaTime * 6f;
                sr.sprite = ((int)animT % 2 == 0) ? walkA : walkB;
            }
            else sr.sprite = idle;
        }
    }
}
