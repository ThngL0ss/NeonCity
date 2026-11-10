#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonBreaker.EditorTools
{
    /// <summary>
    /// Dựng Scene Story (cốt truyện mở đầu 10 cảnh) — chạy trong menu "Cập nhật Nhiệm vụ + Phố Neon + Lưu game".
    /// Scene đã có thì GIỮ NGUYÊN (xoá Story.unity rồi chạy lại menu nếu muốn dựng lại từ đầu).
    ///
    /// Cấu trúc: object "Story" (StoryIntro) có các nhóm con S1_Bunker, S2_Nancelas, S3_Core, S4_Alert, S5_HallA, S5_HallB, S5_Machine,
    /// S7a_Ruins, S7b_Crowd, S8_War, S9_Evo — StoryIntro bật/tắt từng nhóm theo cảnh, nên tất cả nhóm đều tắt sẵn trong Scene.
    /// Tên các object con phải khớp với StoryIntro.cs (xem các T("...") trong đó) — sửa vị trí / thứ tự vẽ thì thoải mái.
    /// Tọa độ viết bằng pixel của khung tranh 480x210 (gốc ở góc trên trái, 16 px = 1 đơn vị): Fq(px, py); hầm 960 px: Fh; ảnh chiến tranh 1280 px: Fw.
    /// Thứ tự vẽ: nền 0 · đồ vật 5-9 · nhân vật 10-14 · hiệu ứng 20+. Hiệu ứng chạy lúc chơi (pháo giấy, tia lửa, mưa, sét...) do StoryIntro tự sinh.
    /// </summary>
    public static partial class NeonBuilder
    {
        static string StoryScenePath { get { return SceneDir + "Story.unity"; } }

        static Vector2 Fq(float px, float py) { return StoryIntro.Fp(px, py); }
        static Vector2 Fh(float px, float py) { return StoryIntro.FpW(px, py, 960f); }
        static Vector2 Fw(float px, float py) { return StoryIntro.FpW(px, py, 1280f); }

        static void BuildStoryScene(Scene scene, string path)
        {
            StoryCamera(Color.black, 0f, -1.875f, false);          // camera đen, không CameraRig: StoryIntro tự điều khiển camera

            var go = new GameObject("Story");
            var si = go.AddComponent<StoryIntro>();
            si.dot = Spr("op_dot");
            si.bolts = Fr("op_bolt_", 3);
            si.boltBig = Spr("op_bolt_big");
            si.rifts = Fr("op_rift_", 3);
            si.rain = Spr("op_rain");

            var t = go.transform;
            StoryS1(t);
            StoryS2(t);
            StoryS3(t);
            StoryS4(t);
            StoryHallA(t);
            StoryHallB(t);
            StoryMachine(t);
            StoryS7a(t);
            var crowd = Group(t, "S7b_Crowd");
            Plain(crowd, "bg", "op_s7b_crowd", Fq(240f, 105f), 0);
            var war = Group(t, "S8_War");
            Plain(war, "bg", "op_s8_war_bg", Fw(640f, 105f), 0);
            StoryS9(t);

            for (int i = 0; i < t.childCount; i++) t.GetChild(i).gameObject.SetActive(false);   // StoryIntro bật từng nhóm khi tới cảnh
            FinishScene(scene, path);
        }

        // ------------------------------------------------------------ tiện ích
        static void StoryScientist(Transform parent, string name, int kind, Vector2 pos, int order)
        {
            string k = "abc".Substring(kind % 3, 1);
            var idle = Clip("idle", 1f, true, Spr("op_sci_" + k + "_idle"));
            var cheer = Clip("cheer", 5f, true, Spr("op_sci_" + k + "_cheer0"), Spr("op_sci_" + k + "_cheer1"));
            Visual(parent.gameObject, name, pos, order, "idle", idle, cheer);
        }

        static SpriteRenderer StoryProp(Transform parent, string name, string sprite, Vector2 pos, int order, bool flip)
        {
            var sr = Plain(parent, name, sprite, pos, order);
            sr.flipX = flip;
            return sr;
        }

        static void StoryFire(Transform parent, string name, Vector2 pos, int order)
        {
            Visual(parent.gameObject, name, pos, order, "fire", Clip("fire", 8f, true, Fr("op_fire_", 3)));
        }

        // ------------------------------------------------------------ cảnh 1: hầm thí nghiệm, cỗ máy Möbius + các nhà khoa học
        static void StoryS1(Transform story)
        {
            var r = Group(story, "S1_Bunker");
            Plain(r, "bg", "op_bunker_bg", Fq(240f, 105f), 0);
            Plain(r, "machine", "op_machine", Fq(240f, 178f), 5);
            var glow = Plain(r, "glow", "op_machine_glow", Fq(240f, 88f), 20, true);
            glow.color = new Color(1f, 1f, 1f, 0f);                          // StoryIntro làm nó chớp sáng tím lúc hết chữ
            var scis = Group(r, "scis");
            int[] xs = { 40, 84, 128, 352, 396, 440, 62, 106, 374, 418 };
            int[] ys = { 192, 192, 192, 192, 192, 192, 206, 206, 206, 206 };
            for (int i = 0; i < xs.Length; i++)
                StoryScientist(scis, "sci_" + i, i, Fq(xs[i], ys[i]), ys[i] > 200 ? 11 : 10);
        }

        // ------------------------------------------------------------ cảnh 2: chỉ còn Nancelas ở lại sửa cỗ máy
        static void StoryS2(Transform story)
        {
            var r = Group(story, "S2_Nancelas");
            var bg = Plain(r, "bg", "op_bunker_bg", Fq(240f, 105f), 0);
            bg.color = new Color(0.6f, 0.6f, 0.68f, 1f);                     // tối hơn: chỉ còn một mình hắn
            var m = Plain(r, "machine", "op_machine", Fq(240f, 178f), 5);
            m.color = new Color(0.75f, 0.75f, 0.85f, 1f);
            Visual(r.gameObject, "nan", Fq(190f, 196f), 12, "work",
                   Clip("idle", 1f, true, Spr("op_nan_idle")),
                   Clip("work", 5f, true, Spr("op_nan_work_0"), Spr("op_nan_work_1")));
        }

        // ------------------------------------------------------------ cảnh 3: khoang lõi — lõi vàng bị thay bằng lõi đen
        static void StoryS3(Transform story)
        {
            var r = Group(story, "S3_Core");
            Plain(r, "bg", "op_s3_bg", Fq(240f, 105f), 0);
            string[] states = { "closed", "yellow", "empty", "black" };
            foreach (var s in states)
            {
                var p = Plain(r, "panel_" + s, "op_s3_panel_" + s, Fq(300f, 105f), 5);
                p.gameObject.SetActive(s == "closed");
            }
            Plain(r, "nan_reach", "op_nan_big_reach", Fq(215f, 200f), 12);
            Plain(r, "nan_hold", "op_nan_big_hold", Fq(215f, 200f), 12).gameObject.SetActive(false);
            Plain(r, "core_y", "op_core_yellow", Fq(300f, 105f), 14).gameObject.SetActive(false);
            Plain(r, "core_b", "op_core_black", Fq(300f, 105f), 14).gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ cảnh 4: các nhà khoa học phát hiện bất thường
        static void StoryS4(Transform story)
        {
            var r = Group(story, "S4_Alert");
            Plain(r, "bg", "op_bunker_bg", Fq(240f, 105f), 0);
            Plain(r, "machine", "op_machine", Fq(240f, 178f), 5);
            var scis = Group(r, "scis");
            var ex = Group(r, "excls");
            int[] xs = { 30, 62, 94, 126, 354, 386, 418, 450 };            // 4 người bên trái rồi 4 người bên phải
            int[] ys = { 190, 200, 190, 200, 190, 200, 190, 200 };
            var blink = Clip("blink", 6f, true, Spr("op_excl_0"), Spr("op_excl_1"));
            for (int i = 0; i < xs.Length; i++)
            {
                StoryScientist(scis, "sci_" + i, i, Fq(xs[i], ys[i]), 10 + (i % 2));
                Visual(ex.gameObject, "excl_" + i, Fq(xs[i] + 8f, ys[i] - 30f), 20, "blink", blink).gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------ cảnh 5a: hầm đổ nát, robot khổng lồ vỡ nát (nhìn nửa phải của hầm 960 px)
        static void StoryHallA(Transform story)
        {
            var r = Group(story, "S5_HallA");
            Plain(r, "bg", "op_hall_bg_a", Fh(480f, 105f), 0);
            StoryProp(r, "wreck_0", "op_mech_xl_wreck_a", Fh(540f, 182f), 8, false);
            StoryProp(r, "wreck_1", "op_mech_xl_wreck_b", Fh(665f, 182f), 8, false);
            StoryProp(r, "light_dent", "op_mech_l_dent", Fh(755f, 182f), 8, false);     // robot 1.5x còn khá nguyên (của Nancelas)
            StoryProp(r, "wreck_2", "op_mech_xl_wreck_a", Fh(850f, 182f), 8, true);
            StoryProp(r, "wreck_3", "op_mech_xl_wreck_b", Fh(935f, 182f), 8, true);
            StoryProp(r, "debris_0", "op_debris_0", Fh(600f, 186f), 7, false);
            StoryProp(r, "debris_1", "op_debris_1", Fh(705f, 188f), 7, false);
            StoryProp(r, "debris_2", "op_debris_2", Fh(805f, 186f), 7, false);
            StoryProp(r, "debris_3", "op_debris_0", Fh(905f, 188f), 7, true);
            StoryFire(r, "fire_0", Fh(600f, 180f), 6);
            StoryFire(r, "fire_1", Fh(820f, 180f), 6);
        }

        // ------------------------------------------------------------ cảnh 5b: sụp đổ nặng hơn, thành phố cháy; camera trôi sang trái tới cỗ máy
        static void StoryHallB(Transform story)
        {
            var r = Group(story, "S5_HallB");
            Plain(r, "bg", "op_hall_bg_b", Fh(480f, 105f), 0);
            // nửa trái (px 0..480): hai bên cỗ máy ở px 240
            StoryProp(r, "mech_l0", "op_mech_xl_ok", Fh(48f, 182f), 8, false);
            StoryProp(r, "mech_l1", "op_mech_xl_dent", Fh(112f, 182f), 8, true);
            StoryProp(r, "dead_l0", "op_sci_dead_0", Fh(160f, 190f), 7, false);
            StoryProp(r, "dead_l1", "op_sci_dead_1", Fh(330f, 190f), 7, true);
            StoryProp(r, "mech_l2", "op_mech_xl_wreck_a", Fh(392f, 182f), 8, false);
            StoryProp(r, "mech_l3", "op_mech_xl_ok", Fh(452f, 182f), 8, true);
            StoryFire(r, "fire_l0", Fh(90f, 180f), 6);
            StoryFire(r, "fire_l1", Fh(350f, 180f), 6);
            StoryFire(r, "fire_l2", Fh(430f, 180f), 6);
            // nửa phải (px 480..960): nơi camera bắt đầu
            StoryProp(r, "mech_r0", "op_mech_xl_ok", Fh(520f, 182f), 8, false);
            StoryProp(r, "mech_r1", "op_mech_xl_dent", Fh(610f, 182f), 8, true);
            StoryProp(r, "mech_r2", "op_mech_xl_wreck_b", Fh(685f, 182f), 8, false);
            StoryProp(r, "nan_ruin", "op_mech_l_ruin", Fh(760f, 182f), 8, false);      // robot 1.5x nát bét, phi công bị thương
            StoryProp(r, "mech_r3", "op_mech_xl_ok", Fh(860f, 182f), 8, true);
            StoryProp(r, "mech_r4", "op_mech_xl_dent", Fh(935f, 182f), 8, false);
            StoryProp(r, "dead_r0", "op_sci_dead_0", Fh(565f, 190f), 7, false);
            StoryProp(r, "dead_r1", "op_sci_dead_2", Fh(645f, 192f), 7, false);
            StoryProp(r, "dead_r2", "op_sci_dead_1", Fh(815f, 190f), 7, true);
            StoryProp(r, "dead_r3", "op_sci_dead_2", Fh(905f, 192f), 7, false);
            StoryProp(r, "debris_0", "op_debris_1", Fh(575f, 187f), 7, false);
            StoryProp(r, "debris_1", "op_debris_2", Fh(725f, 188f), 7, false);
            StoryProp(r, "debris_2", "op_debris_0", Fh(880f, 187f), 7, false);
            StoryFire(r, "fire_r0", Fh(590f, 180f), 6);
            StoryFire(r, "fire_r1", Fh(735f, 180f), 6);
            StoryFire(r, "fire_r2", Fh(830f, 180f), 6);
        }

        // ------------------------------------------------------------ cảnh 5c + 6: cỗ máy trong hầm (px 240), tia vật chất đen, sét, khe nứt; robot lao vào
        static void StoryMachine(Transform story)
        {
            var r = Group(story, "S5_Machine");
            Plain(r, "machine", "op_machine", Fh(240f, 176f), 6);
            var glow = Plain(r, "glow", "op_machine_glow", Fh(240f, 86f), 20, true);
            glow.color = new Color(0.7f, 0.4f, 1f, 0.16f);
            var rays = Group(r, "rays");
            for (int i = 0; i < 8; i++)
            {
                var ray = Plain(rays, "ray_" + i, "op_ray", Fh(240f, 86f), 30, true);
                ray.transform.rotation = Quaternion.Euler(0f, 0f, i * 45f + 12f);
                ray.color = new Color(1f, 1f, 1f, 0f);                       // ẩn tới khi cỗ máy bắt đầu nổi loạn
            }
            Plain(r, "dasher", "op_mech_xl_dash", Fh(520f, 176f), 16).gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ cảnh 7: tàn tích La Mã + nhà chọc trời + xe bay
        static void StoryS7a(Transform story)
        {
            var r = Group(story, "S7a_Ruins");
            Plain(r, "bg", "op_s7a_bg", Fq(240f, 105f), 0);
            Plain(r, "car_0", "op_car_0", Fq(60f, 48f), 20);
            Plain(r, "car_1", "op_car_1", Fq(300f, 36f), 20);
            Plain(r, "car_2", "op_car_0", Fq(180f, 70f), 20);
            Plain(r, "car_3", "op_car_1", Fq(400f, 62f), 20);
        }

        // ------------------------------------------------------------ cảnh 9: tiến hóa từ vượn tới người tương lai + đường kẻ trắng gợn sóng
        static void StoryS9(Transform story)
        {
            var r = Group(story, "S9_Evo");
            float[,] pos = { { 56f, 164f }, { 131f, 112f }, { 206f, 150f }, { 281f, 90f }, { 356f, 124f }, { 425f, 50f } };
            for (int i = 0; i < 6; i++)
                Plain(r, "fig_" + i, "op_evo_" + i, Fq(pos[i, 0], pos[i, 1]), 6);
            for (int i = 0; i < 5; i++)
                Plain(r, "line_" + i, "op_evo_line_" + i, Fq(240f, 105f), 4);
        }
    }
}
#endif
