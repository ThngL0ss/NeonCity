using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Albert kể về các phe phái mà người chơi đã chạm mặt — mở khoá sau khi thám hiểm xong mọi khu vực (Town.AllExplored → Town.FactionStory = Pending),
    /// nghe được khi về căn cứ nói chuyện với Albert (xem BaseManager.TalkToAlbert). Mỗi phe chỉ được kể nếu người chơi đã tới nơi của phe đó.
    /// Lời thoại là BẢN NHÁP do mình ghép từ các dữ kiện tác giả đã viết ở docs/STORY.md mục 4 / 4e / 4f (không thêm chi tiết mới);
    /// tác giả sẽ chỉnh sau — sửa trực tiếp trong <see cref="AddLines"/>.
    /// </summary>
    public static class AlbertFactions
    {
        /// <summary>Có phần kể về các phe đang chờ không (đồng thời mở khoá nếu đã thám hiểm đủ mà chưa có cờ, ví dụ file lưu cũ).</summary>
        public static bool Pending
        {
            get
            {
                if (Town.FactionStory == Town.FactionLocked && Town.AllExplored) Town.FactionStory = Town.FactionPending;
                return Town.FactionStory == Town.FactionPending;
            }
        }

        static void A(List<DlgLine> l, Color a, string vi, string en) { l.Add(new DlgLine("ALBERT", Cfg.L(vi, en), a)); }

        /// <summary>Thêm các câu thoại kể về phe phái vào cuối danh sách; câu cuối đánh dấu đã kể (Town.FactionStory = Done).</summary>
        public static void AddLines(List<DlgLine> l, Color a)
        {
            A(l, a, "Nghe nói cậu đã đi khắp Phố Neon và cả vùng rìa rồi.", "I hear you've been all over Neon Street and the outskirts.");
            A(l, a, "Tốt. Vậy thì cậu cũng nên biết mình vừa chạm mặt những ai.", "Good. Then you should know who you've just crossed paths with.");

            // Pyber / Dyber
            A(l, a, "Trước hết là phe của ta: Pyber, ta cùng vài người lập ra năm 2082. Chủ trương của bọn ta là hòa bình, phát triển công nghệ.",
                    "First, our own: Pyber, which I founded with a few others in 2082. We stand for peace and for developing technology.");
            A(l, a, "Đối nghịch với bọn ta là Dyber, lập năm 2079. Chủ trương chiến tranh, cướp bóc để phát triển công nghệ. Hai phe là kẻ thù đối mặt.",
                    "Opposing us is Dyber, founded in 2079. It believes in war and plunder to advance technology. The two factions are enemies face to face.");
            if (Town.IsExplored(StreetBuildings.LabId))
                A(l, a, "Phòng thí nghiệm ngoài rìa là của chúng — cậu cũng thấy chúng chào đón khách thế nào rồi đấy.",
                        "That laboratory out on the outskirts is theirs — you've seen how they welcome visitors.");
            if (Town.IsExplored(StreetBuildings.MineId))
                A(l, a, "Cái mỏ bỏ hoang kia từng thuộc về Dyber, cho tới khi xảy ra một sự cố. Giờ hang quá tệ, chẳng ai dám vào.",
                        "That abandoned mine once belonged to Dyber, until an accident. The tunnel's in terrible shape now; nobody dares go in.");
            A(l, a, "Và kẻ đang giữ lõi của cậu cũng là người của Dyber. Đừng quên điều đó.",
                    "And the one holding your core is also Dyber's. Don't forget that.");

            // các phe còn lại, chỉ kể những phe người chơi đã tới nơi
            if (Town.IsExplored(CenterShopBuilder.Id))
                A(l, a, "CenterShop do Phe tài phiệt bảo hộ: những kẻ nắm quyền và giàu có từ thế giới cũ, dùng tiền và tài sản để đàn áp, nô dịch phe yếu thế.",
                        "CenterShop is protected by the Tycoon faction: those who held power and wealth in the old world, using money and property to oppress and enslave the weaker factions.");
            if (Town.IsExplored(ZenBuilder.Id))
                A(l, a, "Phe Hòa hảo – ZenCity thì cậu biết rồi: người yêu hòa bình từ cả ba dòng thời gian, luôn muốn đưa thế giới về một trật tự mới.",
                        "The Peace faction – ZenCity you already know: people who love peace from all three timelines, always wanting to bring the world to a new order.");
            if (Town.IsExplored(DesTownBuilder.Id))
                A(l, a, "DesTown4 là một trong bảy trụ sở chính của Phe diệt chủng — do con người tương lai lập, muốn tận diệt loài người ở các mốc thời gian cũ và nắm toàn quyền thế giới.",
                        "DesTown4 is one of the seven main headquarters of the Genocide faction — founded by people of the future, who want to wipe out humanity in the older timelines and seize total control of the world.");
            if (Town.IsExplored(ArenaBuilder.Id))
                A(l, a, "Còn đấu trường ArenaCyber là nơi Phe diệt chủng và Phe tài phiệt cùng chung tay.",
                        "As for ArenaCyber, it's where the Genocide faction and the Tycoon faction join hands.");
            if (Town.IsExplored(MerShopBuilder.Id))
                A(l, a, "Bọn ở MerShop thuộc Phe đánh thuê: nhận tiền làm việc bẩn thỉu trong bóng tối, chỉ cần có tiền.",
                        "The people at MerShop belong to the Mercenary faction: they do dirty work in the dark for money — all they need is payment.");
            if (Town.IsExplored(RyberBuilder.Id))
                A(l, a, "Lều trại ngoài rìa là của Phe trộm cướp – Ryber. Chúng thường đi cướp của kẻ giàu có.",
                        "The camp out on the outskirts belongs to the Thief faction – Ryber. They usually rob the rich.");
            if (Town.IsExplored(StreetBuildings.SlaveId))
            {
                A(l, a, "Còn ngôi nhà gỗ ba tầng ở tận cùng rìa phố, ReWolrld, là trụ sở thứ 3 của Phe nô lệ: những người có tài đứng về phía kẻ yếu bị nô dịch, hay biểu tình, khởi nghĩa để giành tự do và quyền lợi.",
                        "And the three-storey wooden house at the very end of the outskirts, ReWolrld, is the 3rd headquarters of the Slave faction: talented people who stand with the enslaved weak, often protesting and rising up for freedom and rights.");
                A(l, a, "Cửa họ đóng kín, nhưng đèn vẫn sáng. Hiện tại thì cậu chưa vào được.",
                        "Their door is shut tight, yet the lights are still on. For now, you can't get in.");
            }

            // hiệp ước ngầm
            A(l, a, "Dù thù nhau, các phe đều giữ một hiệp ước ngầm: không ai được xen vào sự tồn tại của phe khác.",
                    "Even though they hate one another, all the factions keep a secret treaty: nobody may interfere with another faction's existence.");
            A(l, a, "Tuy vậy… chuyện chống đối và phá hủy hiệp ước vẫn thường xảy ra trong bóng tối.",
                    "Even so… defying and breaking that treaty still happens often, in the dark.");
            var last = new DlgLine("ALBERT", Cfg.L("Ngoài ra còn nhiều phe khác nữa. Rồi cậu sẽ gặp thôi.", "And there are many other factions besides. You'll meet them in time."), a);
            last.onShow = () => { Town.FactionStory = Town.FactionDone; };
            l.Add(last);
        }
    }
}
