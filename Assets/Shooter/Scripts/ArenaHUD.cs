using System.Collections.Generic;
using UnityEngine;

namespace ShooterRange
{
    public sealed class ArenaHUD : MonoBehaviour
    {
        public ArenaGame game;
        GUIStyle style;
        readonly Color panel = new Color(.025f, .045f, .065f, .94f);
        readonly Color orange = new Color(1f, .43f, .14f);
        readonly Color white = new Color(.91f, .95f, .96f);
        readonly Color muted = new Color(.5f, .64f, .69f);
        readonly Color red = new Color(.95f, .22f, .18f);

        void OnGUI()
        {
            if (game == null || game.Human == null) return;
            if (style == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                const string glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÇĞİÖŞÜçğıöşü /:+-.[]>()•";
                // Prepare the dynamic font before drawing, including the first scoreboard frame.
                foreach (int size in new[] {11,12,13,14,16,17,18,20,22,25,26,30,31,32,36,37,44})
                {
                    font.RequestCharactersInTexture(glyphs, size, FontStyle.Normal);
                    font.RequestCharactersInTexture(glyphs, size, FontStyle.Bold);
                }
                style = new GUIStyle(GUI.skin.label) { font = font };
            }
            Matrix4x4 old = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 810f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1440 * scale) / 2, (Screen.height - 810 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            RangeWeapon weapon = game.weapon;
            ArenaCombatant human = game.Human;
            List<ArenaCombatant> ranked = Ranked();
            Box(28, 26, 310, 78, panel); Box(28, 26, 4, 78, orange);
            Text(48, 35, 280, 34, "DEATHMATCH", 25, white, true);
            Text(49, 74, 280, 18, "6 OYUNCU  /  İLK " + game.killLimit + " ÖLDÜRME", 12, muted);
            Box(568, 26, 304, 68, panel);
            Center(32, "SEN  " + human.Kills + " / " + game.killLimit + "     •     LİDER  " + ranked[0].Kills + " / " + game.killLimit, 18, white, true);
            Center(61, Mathf.FloorToInt(game.MatchTime / 60).ToString("00") + ":" + Mathf.FloorToInt(game.MatchTime % 60).ToString("00"), 12, muted);
            for (int i = 0; i < game.Feed.Count; i++)
            {
                Box(1050, 28 + i * 31, 362, 27, panel);
                Text(1064, 28 + i * 31, 345, 27, game.Feed[i].text, 13, game.Feed[i].headshot ? orange : white);
            }
            Box(28, 670, 338, 112, panel);
            Text(48, 681, 110, 24, "SAĞLIK", 12, muted);
            Text(48, 704, 100, 46, human.Health.ToString("000"), 36, human.Health <= 30 ? red : white, true);
            Text(157, 717, 190, 25, human.IsProtected && human.IsAlive ? "DOĞMA KORUMASI" : "TAB  /  SKOR TABLOSU", 12, human.IsProtected ? orange : muted);
            Box(50, 761, 292, 4, new Color(.18f, .24f, .28f));
            Box(50, 761, 292 * human.Health / 100f, 4, human.Health <= 30 ? red : orange);
            Box(1110, 670, 302, 112, panel); Box(1110, 670, 4, 112, orange);
            Text(1132, 683, 250, 20, "P12  /  R İLE ŞARJÖR DEĞİŞTİR", 12, muted);
            Text(1130, 707, 100, 49, weapon.Ammo.ToString("00"), 44, weapon.Ammo <= 3 ? orange : white, true);
            Text(1226, 715, 160, 33, "/ " + weapon.Reserve.ToString("000"), 25, muted);
            for (int i = 0; i < weapon.magazineSize; i++) Box(1134 + i * 21, 767, 15, 4, i < weapon.Ammo ? orange : muted * .35f);
            if (human.IsAlive && !game.IsFinished)
            {
                bool hit = Time.time - weapon.LastHitTime < (weapon.LastHitWasHeadshot ? .35f : .16f);
                float gap = 6 + Mathf.Clamp01(1 - (Time.time - weapon.LastShotTime) * 8) * 5;
                Color cross = hit ? orange : white;
                Box(720 - gap - 7, 404, 7, 2, cross); Box(720 + gap, 404, 7, 2, cross);
                Box(719, 405 - gap - 7, 2, 7, cross); Box(719, 405 + gap, 2, 7, cross);
                if (hit)
                {
                    float length = weapon.LastHitWasHeadshot ? 15 : 9;
                    for (int n = 0; n < 4; n++) Stroke(new Vector2(720, 405) + new Vector2(n % 2 == 0 ? -14 : 14, n < 2 ? -14 : 14), length, n == 0 || n == 3 ? 45 : -45, weapon.LastHitWasHeadshot ? 3 : 2, cross);
                }
                float hsAge = Time.time - weapon.LastHeadshotTime;
                if (hsAge < .9f)
                {
                    Color glow = orange; glow.a = Mathf.Clamp01((.9f - hsAge) * 3f);
                    Center(451 - Mathf.Min(hsAge, .2f) * 30, "HEADSHOT", hsAge < .12f ? 31 : 26, glow, true);
                    Center(485, "+100  /  KAFADAN VURUŞ", 12, glow);
                }
                else if (Time.time - weapon.LastHitTime < .6f && weapon.LastHitWasKnockdown) Center(455, "+1  ÖLDÜRME", 20, orange, true);
                if (weapon.IsReloading)
                {
                    Center(549, "ŞARJÖR DEĞİŞTİRİLİYOR", 13, white);
                    Box(625, 589, 190, 3, panel); Box(625, 589, 190 * weapon.ReloadProgress, 3, orange);
                }
                else if (weapon.Ammo == 0) Center(552, weapon.Reserve > 0 ? "R  /  ŞARJÖR DEĞİŞTİR" : "MÜHİMMAT BİTTİ", 14, orange, true);
            }
            float damage = Mathf.Clamp01(1 - (Time.time - human.LastDamageTime) * 3.5f);
            if (damage > 0)
            {
                Color tint = red; tint.a = damage * .42f;
                Box(0, 0, 1440, 13, tint); Box(0, 797, 1440, 13, tint);
                Box(0, 0, 13, 810, tint); Box(1427, 0, 13, 810, tint);
            }
            bool scoreboard = Input.GetKey(KeyCode.Tab) || game.IsFinished;
            if (!human.IsAlive && !game.IsFinished && !scoreboard)
            {
                Box(400, 275, 640, 230, panel); Box(400, 275, 640, 3, red);
                Center(301, "ELENDİN", 37, red, true);
                Center(364, Mathf.Max(0, human.RespawnAt - game.MatchTime).ToString("0.0") + " SN SONRA YENİDEN DOĞACAKSIN", 16, white);
                Center(421, "TAB ile skor tablosunu görüntüle", 13, muted);
            }
            if (!scoreboard && !game.player.IsControlling && !game.VerificationMode)
            {
                Box(0, 0, 1440, 810, new Color(.01f, .02f, .03f, .6f));
                Box(410, 251, 620, 300, panel); Box(410, 251, 620, 3, orange);
                Center(276, game.HasStarted ? "OYUN DURAKLATILDI" : "ARENAYA HAZIR MISIN?", 32, white, true);
                Center(336, "5 bot. Herkes tek. İlk " + game.killLimit + " öldürme kazanır.", 17, muted);
                Center(386, "SOL TIK  ATEŞ   •   R  ŞARJÖR   •   WASD  HAREKET", 13, white);
                Center(429, "TAB  SKOR   •   ESC  DURAKLAT", 13, muted);
                Center(484, "DEVAM ETMEK İÇİN OYUN ALANINA TIKLA", 16, orange, true);
                Center(757, "ARENA SEED  " + game.map.Seed, 11, muted);
            }
            if (scoreboard) DrawScoreboard(ranked);
            GUI.color = Color.white; GUI.matrix = old;
        }

        List<ArenaCombatant> Ranked()
        {
            var result = new List<ArenaCombatant>(game.Combatants);
            result.Sort((a, b) => a.Kills != b.Kills ? b.Kills.CompareTo(a.Kills) : a.Deaths.CompareTo(b.Deaths));
            return result;
        }

        void DrawScoreboard(List<ArenaCombatant> ranked)
        {
            Box(0, 0, 1440, 810, new Color(.01f, .02f, .03f, .74f));
            Box(340, 152, 760, 515, panel); Box(340, 152, 760, 4, orange);
            Center(170, game.IsFinished ? (game.Winner.isHuman ? "KAZANDIN" : game.Winner.displayName + " KAZANDI") : "SKOR TABLOSU", 30, game.IsFinished ? orange : white, true);
            Center(218, "DEATHMATCH  /  HEDEF " + game.killLimit + " ÖLDÜRME", 13, muted);
            Text(382, 281, 350, 26, "OYUNCU", 13, muted);
            Text(789, 281, 120, 26, "ÖLDÜRME", 13, muted);
            Text(949, 281, 110, 26, "ÖLME", 13, muted);
            for (int i = 0; i < ranked.Count; i++)
            {
                var actor = ranked[i];
                float y = 320 + i * 44;
                if (actor.isHuman) Box(365, y, 710, 40, new Color(.19f, .16f, .12f));
                Text(382, y, 380, 40, (i + 1) + "   " + actor.displayName + (!actor.IsAlive ? "   [ELENDİ]" : ""), 18, actor.isHuman ? orange : white, actor.isHuman);
                Text(815, y, 110, 40, actor.Kills.ToString("00"), 22, white, true);
                Text(965, y, 90, 40, actor.Deaths.ToString("00"), 22, muted);
            }
            Center(607, game.IsFinished ? "ENTER  /  YENİ HARİTADA YENİ MAÇ" : "Geri dönmek için TAB tuşunu bırak", 14, game.IsFinished ? orange : muted, game.IsFinished);
        }

        void Box(float x, float y, float w, float h, Color color) { GUI.color = color; GUI.DrawTexture(new Rect(x,y,w,h), Texture2D.whiteTexture); GUI.color = Color.white; }
        void Text(float x, float y, float w, float h, string text, int size, Color color, bool bold = false)
        { style.fontSize = size; style.normal.textColor = color; style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; style.alignment = TextAnchor.MiddleLeft; GUI.Label(new Rect(x,y,w,h),text,style); }
        void Center(float y, string text, int size, Color color, bool bold = false)
        { style.fontSize = size; style.normal.textColor = color; style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; style.alignment = TextAnchor.MiddleCenter; GUI.Label(new Rect(350,y,740,40),text,style); }
        void Stroke(Vector2 center, float length, float angle, float thickness, Color color)
        {
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(center.x, center.y, 0), Quaternion.Euler(0, 0, angle), Vector3.one);
            Box(-length / 2, -thickness / 2, length, thickness, color);
            GUI.matrix = matrix;
        }
    }
}
