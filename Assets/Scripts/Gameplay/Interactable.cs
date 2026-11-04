using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Mọi thứ người chơi bấm phím tương tác (F) để dùng: rương, cửa, NPC, máy chế tạo, vật phẩm rơi vãi...
    /// Class MonoBehaviour nào cũng có thể implement interface này rồi gọi Interactor.Register / Unregister (thường trong OnEnable / OnDisable).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>false = hiện tại không dùng được (đã mở, đang bận...) nên không hiện gợi ý.</summary>
        bool CanInteract { get; }
        /// <summary>Vị trí thế giới của nhãn gợi ý.</summary>
        Vector2 PromptPos { get; }
        /// <summary>Nội dung nhãn gợi ý, ví dụ "[F] MỞ RƯƠNG".</summary>
        string PromptText { get; }
        /// <summary>Người chơi có đứng trong tầm tương tác không.</summary>
        bool InRange(Player p);
        /// <summary>Thực hiện tương tác (người chơi vừa bấm F).</summary>
        void Interact();
    }

    /// <summary>Quản lý danh sách vật có thể tương tác bằng phím F và chọn vật gần người chơi nhất.</summary>
    public static class Interactor
    {
        static readonly List<IInteractable> all = new List<IInteractable>();

        public static void Register(IInteractable i) { if (i != null && !all.Contains(i)) all.Add(i); }
        public static void Unregister(IInteractable i) { all.Remove(i); }
        public static void ResetStatics() { all.Clear(); }

        /// <summary>Vật tương tác gần người chơi nhất trong tầm (null nếu không có).</summary>
        public static IInteractable Nearest()
        {
            var pl = Player.Instance;
            if (pl == null || pl.Dead) return null;
            IInteractable best = null;
            float bd = 1e9f;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var it = all[i];
                var mb = it as Object;
                if (mb == null) { all.RemoveAt(i); continue; }           // object đã bị huỷ
                if (!it.CanInteract || !it.InRange(pl)) continue;
                float d = Mathf.Abs(pl.Position.x - it.PromptPos.x);
                if (d < bd) { bd = d; best = it; }
            }
            return best;
        }

        /// <summary>Gọi khi người chơi bấm F. Trả về true nếu đã tương tác với một vật.</summary>
        public static bool TryInteract()
        {
            var it = Nearest();
            if (it == null) return false;
            it.Interact();
            return true;
        }

        /// <summary>Tiện ích: người chơi có đứng cách tâm (pos) không quá range theo trục ngang và height theo trục đứng.</summary>
        public static bool Near(Player p, Vector2 pos, float range, float height)
        {
            if (p == null) return false;
            Vector2 d = p.Position - pos;
            return Mathf.Abs(d.x) <= range && Mathf.Abs(d.y) <= height;
        }
    }
}
