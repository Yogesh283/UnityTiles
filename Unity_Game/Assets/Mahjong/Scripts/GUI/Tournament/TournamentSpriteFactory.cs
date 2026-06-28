using UnityEngine;

namespace Mkey.Tournament
{
    public static class TournamentBorderRadius
    {
        /// <summary>Main tournament cards — nearly square corners (reference).</summary>
        public const float Card = 6f;
        /// <summary>JOIN buttons — full capsule / pill shape.</summary>
        public const float PillButton = 58f;
        /// <summary>Status badges — pill shape (capsule ends).</summary>
        public const float Badge = 22f;
        /// <summary>Wallet counter — subtle rounding.</summary>
        public const float Wallet = 10f;
        /// <summary>Info / type tiles — match cards.</summary>
        public const float Panel = 6f;
    }

    /// <summary>
    /// Procedural premium 9-slice sprites — AAA mobile game styling.
    /// </summary>
    public static class TournamentSpriteFactory
    {
        private const int TexSize = 128;

        private static Sprite _cardBg;
        private static Sprite _buttonOrange;
        private static Sprite _buttonGreen;
        private static Sprite _goldFrame;
        private static Sprite _hexFrame;
        private static Sprite _badge;
        private static Sprite _wallet;
        private static Sprite _circle;
        private static Sprite _backCircle;
        private static Sprite _glassSheen;
        private static Sprite _bambooStripe;

        public static Sprite CardBackground => _cardBg ?? (_cardBg = BuildCardBackground());
        public static Sprite ButtonOrange => _buttonOrange ?? (_buttonOrange = BuildOrangeButton());
        public static Sprite ButtonGreen => _buttonGreen ?? (_buttonGreen = BuildGreenButton());
        public static Sprite GoldFrame => _goldFrame ?? (_goldFrame = BuildGoldFrame());
        public static Sprite HexFrame => _hexFrame ?? (_hexFrame = BuildHexFrame());
        public static Sprite Badge => _badge ?? (_badge = BuildBadge());
        public static Sprite Wallet => _wallet ?? (_wallet = BuildWallet());
        public static Sprite SoftCircle => _circle ?? (_circle = BuildCircle(128));
        public static Sprite BackCircle => _backCircle ?? (_backCircle = BuildBackCircle());
        public static Sprite GlassSheen => _glassSheen ?? (_glassSheen = BuildGlassSheen());
        public static Sprite BambooStripe => _bambooStripe ?? (_bambooStripe = BuildBambooStripe());

        private static Vector4 CardBorders => new Vector4(12f, 12f, 12f, 12f);
        private static Vector4 PillBorders => new Vector4(32f, 32f, 32f, 32f);
        private static Vector4 BadgeBorders => new Vector4(24f, 24f, 24f, 24f);

        private static Sprite BuildCardBackground()
        {
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color top = new Color(0.08f, 0.26f, 0.19f, 1f);
            Color mid = new Color(0.05f, 0.16f, 0.12f, 1f);
            Color bottom = new Color(0.02f, 0.08f, 0.06f, 1f);
            Color gold = new Color(0.95f, 0.8f, 0.32f, 1f);
            Color goldHi = new Color(1f, 0.92f, 0.55f, 1f);
            Color goldDark = new Color(0.5f, 0.38f, 0.1f, 1f);
            float r = TournamentBorderRadius.Card;
            for (int y = 0; y < TexSize; y++)
            for (int x = 0; x < TexSize; x++)
            {
                float t = y / (float)TexSize;
                Color fill = t < 0.5f ? Color.Lerp(bottom, mid, t * 2f) : Color.Lerp(mid, top, (t - 0.5f) * 2f);
                float a = RoundedRectAlpha(x, y, TexSize, TexSize, r);
                if (a <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }

                bool outerGold = x < 2f || y < 2f || x >= TexSize - 3f || y >= TexSize - 3f;
                bool midGold = x < 5f || y < 5f || x >= TexSize - 6f || y >= TexSize - 6f;
                Color c = fill;
                if (outerGold) c = goldHi;
                else if (midGold) c = Color.Lerp(goldDark, gold, 0.55f);

                // Glass highlight upper third
                if (!outerGold && !midGold && y > TexSize * 0.52f)
                {
                    float sheen = Mathf.Clamp01((y - TexSize * 0.52f) / (TexSize * 0.35f));
                    float band = Mathf.Exp(-Mathf.Pow((x - TexSize * 0.35f) / (TexSize * 0.28f), 2f) * 2.5f);
                    c = Color.Lerp(c, new Color(0.35f, 0.75f, 0.52f, 1f), sheen * band * 0.22f);
                }

                c.a = a;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, CardBorders);
        }

        private static Sprite BuildOrangeButton()
        {
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color top = new Color(1f, 0.62f, 0.18f, 1f);
            Color mid = new Color(0.92f, 0.45f, 0.08f, 1f);
            Color bottom = new Color(0.58f, 0.22f, 0.04f, 1f);
            Color gold = new Color(0.98f, 0.84f, 0.38f, 1f);
            Color goldDark = new Color(0.55f, 0.42f, 0.12f, 1f);
            float r = Mathf.Min(TournamentBorderRadius.PillButton, TexSize * 0.5f - 2f);
            for (int y = 0; y < TexSize; y++)
            for (int x = 0; x < TexSize; x++)
            {
                float t = y / (float)TexSize;
                Color fill = t < 0.45f ? Color.Lerp(bottom, mid, t / 0.45f) : Color.Lerp(mid, top, (t - 0.45f) / 0.55f);
                float a = RoundedRectAlpha(x, y, TexSize, TexSize, r);
                if (a <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }

                bool outer = x < 3f || y < 3f || x >= TexSize - 4f || y >= TexSize - 4f;
                bool inner = x < 6f || y < 6f || x >= TexSize - 7f || y >= TexSize - 7f;
                Color c = fill;
                if (outer) c = gold;
                else if (inner) c = Color.Lerp(goldDark, fill, 0.25f);
                else if (y > TexSize * 0.58f)
                    c = Color.Lerp(c, Color.white, 0.18f);

                c.a = a;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, PillBorders);
        }

        private static Sprite BuildGreenButton()
        {
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color top = new Color(0.28f, 0.9f, 0.5f, 1f);
            Color mid = new Color(0.14f, 0.65f, 0.36f, 1f);
            Color bottom = new Color(0.06f, 0.35f, 0.2f, 1f);
            Color gold = new Color(0.98f, 0.84f, 0.38f, 1f);
            Color goldDark = new Color(0.55f, 0.42f, 0.12f, 1f);
            float r = Mathf.Min(TournamentBorderRadius.PillButton, TexSize * 0.5f - 2f);
            for (int y = 0; y < TexSize; y++)
            for (int x = 0; x < TexSize; x++)
            {
                float t = y / (float)TexSize;
                Color fill = t < 0.45f ? Color.Lerp(bottom, mid, t / 0.45f) : Color.Lerp(mid, top, (t - 0.45f) / 0.55f);
                float a = RoundedRectAlpha(x, y, TexSize, TexSize, r);
                if (a <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }
                bool outer = x < 3f || y < 3f || x >= TexSize - 4f || y >= TexSize - 4f;
                bool inner = x < 6f || y < 6f || x >= TexSize - 7f || y >= TexSize - 7f;
                Color c = fill;
                if (outer) c = gold;
                else if (inner) c = Color.Lerp(goldDark, fill, 0.25f);
                else if (y > TexSize * 0.58f) c = Color.Lerp(c, Color.white, 0.2f);
                c.a = a;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, PillBorders);
        }

        private static Sprite BuildGoldFrame()
        {
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color gold = new Color(0.98f, 0.86f, 0.42f, 1f);
            Color dark = new Color(0.42f, 0.32f, 0.08f, 1f);
            float r = TournamentBorderRadius.Panel;
            for (int y = 0; y < TexSize; y++)
            for (int x = 0; x < TexSize; x++)
            {
                float outer = RoundedRectAlpha(x, y, TexSize, TexSize, r);
                float inner = RoundedRectAlpha(x - 7f, y - 7f, TexSize - 14, TexSize - 14, r - 4f);
                if (outer <= 0f || inner >= outer) { tex.SetPixel(x, y, Color.clear); continue; }
                float t = y / (float)TexSize;
                Color c = Color.Lerp(dark, gold, Mathf.Lerp(t, 1f - t, 0.5f));
                if (x < 8 || y < 8) c = Color.Lerp(c, gold, 0.35f);
                c.a = outer;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, CardBorders);
        }

        private static Sprite BuildHexFrame()
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.47f;
            Color goldHi = new Color(1f, 0.9f, 0.5f, 1f);
            Color gold = new Color(0.92f, 0.74f, 0.26f, 1f);
            Color goldDark = new Color(0.45f, 0.34f, 0.08f, 1f);
            Color inner = new Color(0.04f, 0.12f, 0.09f, 1f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = HexDistance(new Vector2(x + 0.5f, y + 0.5f), c, radius);
                if (d > 1f) { tex.SetPixel(x, y, Color.clear); continue; }
                if (d > 0.86f) tex.SetPixel(x, y, y > c.y ? goldHi : gold);
                else if (d > 0.78f) tex.SetPixel(x, y, goldDark);
                else tex.SetPixel(x, y, inner);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildBadge()
        {
            const int w = 128;
            const int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float r = TournamentBorderRadius.Badge;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = RoundedRectAlpha(x, y, w, h, r);
                float t = y / (float)h;
                Color c = Color.Lerp(new Color(1f, 1f, 1f, 0.9f), new Color(0.88f, 0.88f, 0.88f, 1f), t);
                c.a = a;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, BadgeBorders);
        }

        private static Sprite BuildWallet()
        {
            Texture2D tex = new Texture2D(TexSize, 72, TextureFormat.RGBA32, false);
            Color top = new Color(0.09f, 0.24f, 0.17f, 1f);
            Color bottom = new Color(0.03f, 0.1f, 0.07f, 1f);
            Color gold = new Color(0.95f, 0.8f, 0.32f, 1f);
            float r = TournamentBorderRadius.Wallet;
            for (int y = 0; y < 72; y++)
            for (int x = 0; x < TexSize; x++)
            {
                float a = RoundedRectAlpha(x, y, TexSize, 72, r);
                if (a <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }
                float t = y / 72f;
                Color fill = Color.Lerp(bottom, top, t);
                bool border = x < 2 || y < 2 || x >= TexSize - 3 || y >= 70;
                Color c = border ? gold : fill;
                if (!border && y > 50) c = Color.Lerp(c, new Color(0.2f, 0.5f, 0.35f, 0.3f), 0.25f);
                c.a = a;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, TexSize, 72), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 12, 16, 12));
        }

        private static Sprite BuildBackCircle()
        {
            Texture2D tex = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            float c = 48f;
            for (int y = 0; y < 96; y++)
            for (int x = 0; x < 96; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                if (d > 1f) { tex.SetPixel(x, y, Color.clear); continue; }
                Color fill = new Color(0.06f, 0.18f, 0.13f, 1f);
                if (d > 0.9f) tex.SetPixel(x, y, new Color(0.95f, 0.8f, 0.32f, 1f));
                else if (d > 0.78f) tex.SetPixel(x, y, Color.Lerp(fill, new Color(0.35f, 0.28f, 0.08f, 1f), 0.4f));
                else if (y > c + 4f) tex.SetPixel(x, y, Color.Lerp(fill, Color.black, 0.15f));
                else if (y < c - 8f) tex.SetPixel(x, y, Color.Lerp(fill, Color.white, 0.08f));
                else tex.SetPixel(x, y, fill);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildGlassSheen()
        {
            Texture2D tex = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 128; x++)
            {
                float nx = x / 128f;
                float ny = y / 64f;
                float band = Mathf.Exp(-Mathf.Pow((nx - 0.35f + ny * 0.4f) / 0.22f, 2f));
                float a = band * (1f - ny * 0.5f) * 0.35f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 128, 64), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildBambooStripe()
        {
            Texture2D tex = new Texture2D(32, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 32; x++)
            {
                float stripe = Mathf.PerlinNoise(x * 0.15f, y * 0.02f);
                float edge = 1f - Mathf.Abs(x - 16f) / 16f;
                float a = edge * (0.12f + stripe * 0.18f);
                tex.SetPixel(x, y, new Color(0.02f, 0.08f, 0.05f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 256), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildCircle(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float RoundedRectAlpha(float x, float y, float w, float h, float r)
        {
            float ix = Mathf.Clamp(x, r, w - r - 1);
            float iy = Mathf.Clamp(y, r, h - r - 1);
            float dx = x - ix;
            float dy = y - iy;
            return Mathf.Sqrt(dx * dx + dy * dy) <= r ? 1f : 0f;
        }

        private static float HexDistance(Vector2 p, Vector2 center, float radius)
        {
            Vector2 d = p - center;
            return Mathf.Max(Mathf.Abs(d.x) * 0.866025f + Mathf.Abs(d.y) * 0.5f, Mathf.Abs(d.y)) / radius;
        }
    }
}
