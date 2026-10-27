using UnityEngine;

namespace ShooterRange
{
    public sealed class RangeHUD : MonoBehaviour
    {
        public RangeWeapon weapon;
        public RangePlayer player;
        readonly Color ink = new Color(.035f, .06f, .08f, .91f);
        readonly Color orange = new Color(1f, .43f, .16f);
        readonly Color white = new Color(.9f, .95f, .96f);
        readonly Color muted = new Color(.52f, .65f, .69f);
        GUIStyle label;

        void OnGUI()
        {
            if (weapon == null || player == null) return;
            if (label == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleLeft };
            }
            Matrix4x4 oldMatrix = GUI.matrix;
            float s = Mathf.Min(Screen.width / 1440f, Screen.height / 810f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1440f * s) * .5f, (Screen.height - 810f * s) * .5f), Quaternion.identity, Vector3.one * s);
            Box(32, 30, 318, 80, ink);
            Box(32, 30, 4, 80, orange);
            Text(52, 41, 280, 30, "POLİGON / 01", 25, white, FontStyle.Bold);
            Text(53, 76, 270, 20, "KÜBİK ATIŞ ALANI     •     9 MM", 12, muted);
            Box(1148, 30, 260, 80, ink);
            Text(1170, 40, 100, 20, "İSABET", 12, muted);
            Text(1300, 40, 100, 20, "HEDEF", 12, muted);
            Text(1170, 60, 100, 36, weapon.Hits.ToString("00"), 30, white, FontStyle.Bold);
            Text(1300, 60, 100, 36, weapon.Knockdowns.ToString("00"), 30, orange, FontStyle.Bold);
            Box(32, 698, 368, 80, ink);
            Text(51, 708, 340, 23, "SOL TIK  ATEŞ     /     R  ŞARJÖR", 14, white, FontStyle.Bold);
            Text(51, 740, 340, 22, "WASD  HAREKET     T  İKMAL     ESC  İMLEÇ", 11, muted);
            Box(1110, 666, 298, 112, ink);
            Box(1110, 666, 4, 112, orange);
            Text(1132, 678, 245, 20, "P12  /  OTOMATİK", 12, muted);
            Text(1130, 701, 110, 55, weapon.Ammo.ToString("00"), 48, weapon.Ammo <= 3 ? orange : white, FontStyle.Bold);
            Text(1220, 714, 170, 34, "/ " + weapon.Reserve.ToString("000"), 24, muted);
            for (int i = 0; i < weapon.magazineSize; i++) Box(1134 + i * 21, 762, 15, 4, i < weapon.Ammo ? orange : new Color(.2f, .28f, .31f));
            bool hit = Time.time - weapon.LastHitTime < .16f;
            Color cross = hit ? orange : white;
            float gap = 6f + Mathf.Clamp01(1f - (Time.time - weapon.LastShotTime) * 8f) * 6f;
            Box(720 - gap - 7, 404, 7, 2, cross); Box(720 + gap, 404, 7, 2, cross);
            Box(719, 405 - gap - 7, 2, 7, cross); Box(719, 405 + gap, 2, 7, cross);
            Box(719, 404, 2, 2, cross);
            if (hit) Center(437, weapon.LastHitWasKnockdown ? "HEDEF DÜŞTÜ" : "İSABET", 12, orange);
            if (weapon.IsReloading)
            {
                Center(510, "ŞARJÖR DEĞİŞTİRİLİYOR", 14, white);
                Box(620, 544, 200, 3, ink);
                Box(620, 544, 200 * weapon.ReloadProgress, 3, orange);
            }
            else if (Time.time < weapon.NoticeUntil) Center(526, weapon.Notice, 14, orange);
            else if (weapon.Ammo == 0) Center(526, weapon.Reserve > 0 ? "R  /  ŞARJÖR DEĞİŞTİR" : "T  /  MÜHİMMAT İKMALİ", 14, orange);
            if (!player.IsControlling)
            {
                Box(0, 0, 1440, 810, new Color(.015f, .025f, .035f, .68f));
                Box(470, 270, 500, 258, ink);
                Box(470, 270, 500, 3, orange);
                Center(290, "ATIŞA HAZIR", 34, white, FontStyle.Bold);
                Center(345, "Nişan al. Ateş et. Şarjörünü yenile.", 17, muted);
                Center(380, "Hedefler 3 isabette düşer ve yeniden kurulur.", 14, muted);
                Center(437, "BAŞLAMAK İÇİN OYUN ALANINA TIKLA", 16, orange, FontStyle.Bold);
                Center(481, "ESC ile imleci serbest bırak", 12, muted);
            }
            GUI.color = Color.white;
            GUI.matrix = oldMatrix;
        }
        void Box(float x, float y, float w, float h, Color color) { GUI.color = color; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = Color.white; }
        void Text(float x, float y, float w, float h, string text, int size, Color color, FontStyle style = FontStyle.Normal)
        { label.fontSize = size; label.normal.textColor = color; label.fontStyle = style; label.alignment = TextAnchor.MiddleLeft; GUI.Label(new Rect(x, y, w, h), text, label); }
        void Center(float y, string text, int size, Color color, FontStyle style = FontStyle.Normal)
        { label.fontSize = size; label.normal.textColor = color; label.fontStyle = style; label.alignment = TextAnchor.MiddleCenter; GUI.Label(new Rect(360, y, 720, 42), text, label); }
    }
}
