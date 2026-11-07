using System.Collections;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Các kịch bản cinematic có sẵn (chạy bằng Cine.Play): cảnh mở đầu khi bấm Chơi và cảnh người chơi chết.
    /// Muốn đổi lời thoại / thời gian thì sửa ở đây. Tên vật phẩm bậc SS bị cướp: StolenItemVi / StolenItemEn.
    /// </summary>
    public static class CineScripts
    {
        // ====== TÊN VẬT PHẨM BẬC SS (bạn có thể đổi) ======
        public const string StolenItemVi = "LÕI TỐI THƯỢNG";
        public const string StolenItemEn = "PRIME CORE";
        public const string HeroName = "XbAst-013";

        public static string StolenItem { get { return Cfg.L(StolenItemVi, StolenItemEn); } }

        static float Dt { get { return Time.unscaledDeltaTime; } }

        // ================================================================ MỞ ĐẦU
        /// <summary>Cảnh mở đầu: camera bay tới địa bàn boss (chỉ một bóng đen của boss + linh kiện SS bị cướp phát sáng), thoại mục đích, rồi quay về người chơi.</summary>
        public static IEnumerator Intro()
        {
            var pl = Player.Instance;
            Vector2 arena = new Vector2(Cfg.AltarX, 3.2f);

            Cine.SetFade(1f, 0f);
            Cine.SetBars(1f, 0.01f);
            Cine.Focus(arena, 1f);
            if (CameraRig.Inst != null) CameraRig.Inst.Snap();
            Sfx.Play("warn", 0.5f, 0.5f);

            // dựng cảnh: bóng boss + vầng sáng + lõi SS bị cướp
            var stage = new GameObject("CineIntroStage");
            var sil = BuildSilhouette(stage.transform, new Vector2(Cfg.AltarX, 0f));
            var core = BuildStolenCore(stage.transform, new Vector2(Cfg.AltarX + 3.2f, 4.2f));
            GameManager.Inst.StartCoroutine(PropLoop(stage, core));

            Cine.SetFade(0f, 1.2f);
            yield return Cine.Wait(1.3f);

            yield return Cine.Say(Cfg.L("XbAst-013", "XbAst-013"),
                Cfg.L("Địa bàn của OVERLORD... cuối cùng mình cũng tới được đây.", "OVERLORD's territory... I finally made it here."), 1.1f);
            yield return Cine.Say(HeroName,
                Cfg.L("Nó đã cướp khỏi mình linh kiện bậc SS — " + StolenItem + ".", "It stole my SS-rank part from me — the " + StolenItem + "."), 1.3f);
            Fx.Flare(core.position, PowerRules.RankColor(Rank.SS), 4f, 0.5f);
            Sfx.Play("charge", 0.6f, 1.4f);
            yield return Cine.Say(HeroName,
                Cfg.L("Thiếu nó mình chỉ là một cỗ máy rỗng. Hôm nay mình sẽ lấy lại — dù phải phá nát nơi này.", "Without it I'm just an empty machine. Today I take it back — even if I have to tear this place apart."), 1.4f);
            Cine.ClearText();

            // quay về chỗ người chơi
            if (pl != null) Cine.Focus(pl.Center + new Vector2(0f, 1f), 5f);
            yield return Cine.Wait(2.2f);
            yield return Cine.Say(HeroName, Cfg.L("Phá hết trụ triệu hồi, mở cổng, rồi hạ nó. Đi thôi.", "Smash the summon towers, open the gate, take it down. Let's go."), 0.9f);
            Cine.ClearText();

            Cine.SetBars(0f, 0.6f);
            Cine.ReleaseFocus();
            Cine.CamSmooth = 1f;
            Object.Destroy(stage);
            yield return Cine.Wait(0.5f);
        }

        static Transform BuildSilhouette(Transform parent, Vector2 pos)
        {
            Art.Init();
            var root = new GameObject("BossSilhouette").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(pos.x, pos.y, 0f);
            Sprite s = null;
            Vector3 lp = Vector3.zero, ls = Vector3.one;
            var prefab = GameManager.Inst != null ? GameManager.Inst.bossPrefab : null;
            if (prefab == null && Art.Bank != null) prefab = Art.Bank.bossPrefab;
            if (prefab != null)
            {
                var ph = prefab.transform.Find("Visual/Phase1");
                if (ph != null)
                {
                    lp = ph.localPosition; ls = ph.localScale;
                    var sa = ph.GetComponent<SpriteAnimator>();
                    if (sa != null && sa.clips != null)
                        for (int i = 0; i < sa.clips.Length && s == null; i++)
                            if (sa.clips[i] != null && sa.clips[i].name == "idle" && sa.clips[i].frames != null && sa.clips[i].frames.Length > 0) s = sa.clips[i].frames[0];
                    if (s == null && sa != null && sa.clips != null && sa.clips.Length > 0 && sa.clips[0].frames != null && sa.clips[0].frames.Length > 0) s = sa.clips[0].frames[0];
                }
            }
            if (s != null)
            {
                var sr = Art.Spr(root, "Body", s, new Vector2(lp.x, lp.y), new Vector2(-ls.x, ls.y), Color.black, 30);
                sr.transform.localScale = new Vector3(-Mathf.Abs(ls.x), ls.y, 1f);   // quay mặt về phía người chơi (bên trái)
            }
            return root;      // chỉ có MỘT bóng đen (không vầng sáng, không viền, không mắt)
        }

        static Transform BuildStolenCore(Transform parent, Vector2 pos)
        {
            Art.Init();
            var root = new GameObject("StolenCore").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(pos.x, pos.y, 0f);
            Color gold = PowerRules.RankColor(Rank.SS);
            Sprite ic = Art.Bank != null && Art.Bank.partIcon != null ? Art.Bank.partIcon : Art.Diamond;
            float sz = ic == Art.Diamond ? 1.2f : 3.2f;
            Art.Spr(root, "Glow", Art.Glow, Vector2.zero, new Vector2(7f, 7f), Cfg.A(gold, 0.5f), 33, true);
            Art.Spr(root, "Icon", ic, Vector2.zero, new Vector2(sz, sz), Color.Lerp(gold, Color.white, 0.35f), 34);
            return root;
        }

        /// <summary>Làm lõi SS nhấp nhô + toả hạt cho đến khi cinematic kết thúc rồi dọn cảnh.</summary>
        static IEnumerator PropLoop(GameObject stage, Transform core)
        {
            float t = 0f, nextFx = 0f;
            Vector3 b = core.position;
            Color gold = PowerRules.RankColor(Rank.SS);
            while (Cine.Active && stage != null && core != null)
            {
                t += Dt;
                core.position = b + new Vector3(0f, 0.3f * Mathf.Sin(t * 2.2f), 0f);
                if (t >= nextFx)
                {
                    nextFx = t + 0.16f;
                    Fx.Emit(Fx.S_Glow, (Vector2)core.position + Random.insideUnitCircle * 1.3f, new Vector2(0f, Random.Range(0.6f, 2f)),
                            new Vector2(0.3f, 0.3f), Vector2.zero, Cfg.A(gold, 0.9f), Cfg.A(gold, 0f), 0.8f);
                    if (Random.value < 0.15f) Fx.Ring(core.position, Cfg.A(gold, 0.8f), 1f, 6f, 0.9f);
                }
                yield return null;
            }
            if (stage != null) Object.Destroy(stage);
        }

        // ================================================================ NGƯỜI CHƠI CHẾT
        /// <summary>Người chơi gục xuống, thân xác tan từng điểm ảnh để lộ bộ khung robot, bộ khung vỡ thành mảnh linh kiện rồi màn hình tối dần.</summary>
        public static IEnumerator Death()
        {
            var pl = Player.Instance;
            Cine.SetBars(1f, 0.7f);
            if (pl == null) { Cine.SetFade(1f, 1f); yield return Cine.Wait(1.1f); yield break; }

            Cine.Focus(pl.Center + new Vector2(0f, 0.2f), 2.5f);
            var skin = pl.FreezeVisual();
            Transform vt = pl.visual;
            if (skin == null || vt == null) { Cine.SetFade(1f, 1f); yield return Cine.Wait(1.1f); yield break; }

            yield return Cine.Wait(0.4f);

            // ---- 1. gục xuống
            float dir = pl.Facing >= 0 ? 1f : -1f;
            Vector3 p0 = vt.localPosition;
            Sfx.Play("hurt", 0.8f, 0.55f);
            float t = 0f;
            const float fall = 0.9f;
            while (t < fall && !Cine.Skipping)
            {
                t += Dt;
                float u = Mathf.Clamp01(t / fall);
                float e = u * u * (3f - 2f * u);
                vt.localRotation = Quaternion.Euler(0f, 0f, dir * 84f * e);
                vt.localPosition = p0 + new Vector3(-dir * 0.5f * e, 0.18f * e, 0f);
                if (Random.value < 0.5f) Fx.Sparks(pl.Center, new Vector2(-dir, 0.5f), Cfg.Cyan, 2, 6f, 60f, 0.25f);
                yield return null;
            }
            vt.localRotation = Quaternion.Euler(0f, 0f, dir * 84f);
            vt.localPosition = p0 + new Vector3(-dir * 0.5f, 0.18f, 0f);
            Sfx.Play("land", 0.8f, 0.6f);
            Fx.Shake(0.35f);
            Fx.Ring(pl.Position + new Vector2(0f, 0.1f), Cfg.A(Cfg.Cyan, 0.7f), 0.5f, 3.5f, 0.4f);
            Fx.Burst(pl.Position + new Vector2(0f, 0.2f), Cfg.A(Cfg.Cyan, 0.8f), 12, 5f, 0.4f, 0.3f, 6f);
            yield return Cine.Wait(0.9f);

            // ---- 2. thân xác tan, lộ bộ khung robot
            var bank = Art.Bank;
            SpriteRenderer skelSr = null;
            if (bank != null && bank.playerSkeleton != null)
            {
                var sg = new GameObject("Skeleton");
                sg.transform.SetParent(vt, false);
                skelSr = sg.AddComponent<SpriteRenderer>();
                skelSr.sprite = bank.playerSkeleton;
                skelSr.sortingOrder = skin.sortingOrder - 1;
                skelSr.sharedMaterial = skin.sharedMaterial;
            }
            var dSkin = PixelDissolve.Begin(skin, false, 0.4f, true);
            if (dSkin != null)
            {
                Sfx.Play("charge", 0.5f, 0.6f);
                const float dur = 2.0f;
                t = 0f;
                while (t < dur && !Cine.Skipping)
                {
                    t += Dt;
                    dSkin.Step(t / dur, 9, 3.0f, true);        // cứ 9 điểm ảnh mới rơi 1 hạt (trước đây 2: quá nhiều)
                    if (Random.value < 0.2f) Fx.Sparks(dSkin.RandomPoint(), Vector2.up, Cfg.Cyan, 1, 5f, 90f, 0.3f);
                    yield return null;
                }
                dSkin.Step(1f, 0);
            }
            else
            {
                Fx.Explosion(pl.Center, Cfg.Cyan, true);
                yield return Cine.Wait(0.6f);
            }

            // ---- 3. bộ khung nằm lại, lõi nhấp nháy rồi tắt
            if (skelSr != null)
            {
                t = 0f;
                while (t < 1.0f && !Cine.Skipping)
                {
                    t += Dt;
                    float flick = Mathf.Abs(Mathf.Sin(t * 14f)) * Mathf.Clamp01(1.3f - t);
                    skelSr.color = new Color(1f, 1f, 1f, 1f);
                    if (Random.value < 0.3f) Fx.Sparks(pl.Center, Vector2.up, Cfg.Cyan, 2, 6f, 120f, 0.25f);
                    if (Random.value < 0.2f) Fx.Flare(pl.Center, Cfg.A(Cfg.Cyan, 0.5f + 0.5f * flick), 1.6f * flick + 0.4f, 0.12f);
                    yield return null;
                }
                // ---- 4. bộ khung vỡ thành mảnh linh kiện
                var dSkel = PixelDissolve.Begin(skelSr, false, 0.85f, false);
                Sfx.Play("enemyDie", 0.7f, 0.5f);
                Fx.Explosion(pl.Center, Cfg.Cyan, false);
                const float dur2 = 1.3f;
                t = 0f;
                float bitT = 0f;
                while (t < dur2 && !Cine.Skipping)
                {
                    t += Dt;
                    if (dSkel != null) dSkel.Step(t / dur2, 6, 4.5f, false);          // ít hạt rơi hơn trước
                    bitT -= Dt;
                    if (bitT <= 0f)                                                   // chỉ vài mảnh linh kiện lớn (~12 mảnh), không còn mưa mảnh
                    {
                        bitT = 0.11f;
                        Vector2 p = dSkel != null ? dSkel.RandomPoint() : pl.Center;
                        RobotBit.Spawn(p, new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(2f, 7f)), Random.Range(1.2f, 2.0f));
                    }
                    yield return null;
                }
                if (dSkel != null) { dSkel.Step(1f, 0); dSkel.End(false); }
                Object.Destroy(skelSr.gameObject);
            }
            if (dSkin != null) dSkin.End(false);

            // ---- 5. tối dần
            yield return Cine.Wait(0.8f);
            Cine.SetFade(1f, 1.3f);
            yield return Cine.Wait(1.5f);
        }
    }
}
