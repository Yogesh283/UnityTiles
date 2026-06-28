using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    public static class TournamentPremiumTheme
    {
        public static readonly Color EmeraldDark = new Color(0.015f, 0.06f, 0.045f, 1f);
        public static readonly Color EmeraldVip = new Color(0.04f, 0.14f, 0.1f, 1f);
        public static readonly Color Gold = new Color(0.98f, 0.84f, 0.38f, 1f);
        public static readonly Color GoldBright = new Color(1f, 0.93f, 0.55f, 1f);
        public static readonly Color GoldLabel = new Color(0.82f, 0.68f, 0.3f, 1f);
        public static readonly Color VipAccent = new Color(0.72f, 0.38f, 0.95f, 1f);
        public static readonly Color TextWhite = new Color(0.99f, 0.98f, 0.96f, 1f);
        public static readonly Color TextSoft = new Color(0.84f, 0.9f, 0.86f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.7f, 0.66f, 1f);
        public static readonly Color BadgeOpen = new Color(0.08f, 0.22f, 0.14f, 1f);
        public static readonly Color BadgeFilling = new Color(0.96f, 0.54f, 0.1f, 1f);
        public static readonly Color BadgeStartingSoon = new Color(0.2f, 0.54f, 0.92f, 1f);
        public static readonly Color BadgeFull = new Color(0.9f, 0.24f, 0.2f, 1f);

        public static float CardHeight => TournamentLayoutMetrics.S(TournamentLayoutMetrics.Compact ? 300f : 318f);
        public static float CardSpacing => TournamentLayoutMetrics.S(20f);
        public static float HeaderHeight => TournamentLayoutMetrics.S(460f);
        public static float HorizontalPadding => TournamentLayoutMetrics.S(24f);
        public static float JoinButtonHeight => TournamentLayoutMetrics.S(64f);
        public static int JoinButtonFontSize => TournamentLayoutMetrics.Font(23f);
        public static float DialogButtonHeight => TournamentLayoutMetrics.S(56f);
        public static int DialogButtonFontSize => TournamentLayoutMetrics.Font(21f);
        public static float IconSize => TournamentLayoutMetrics.S(TournamentLayoutMetrics.Compact ? 88f : 104f);
        public static float BadgeHeight => TournamentLayoutMetrics.S(32f);
        public static float SectionTitleHeight => TournamentLayoutMetrics.S(56f);
        public static float InfoCardHeight => TournamentLayoutMetrics.S(188f);
    }

    public static class TournamentPremiumUI
    {
        public static void SetupBackground(Transform parent)
        {
            RectTransform root = TournamentUIFactory.CreateRect(parent, "Background");
            TournamentUIFactory.StretchRect(root);

            Image deep = TournamentUIFactory.CreateImage(root, "Deep", TournamentPremiumTheme.EmeraldDark);
            TournamentUIFactory.StretchRect(deep.rectTransform);

            Sprite pageDesign = TournamentUITheme.PageDesign;
            if (pageDesign)
            {
                Image design = TournamentUIFactory.CreateImage(root, "PageDesign", new Color(1f, 1f, 1f, 0.18f), pageDesign);
                TournamentUIFactory.StretchRect(design.rectTransform);
                design.preserveAspect = false;
            }

            Image vipWash = TournamentUIFactory.CreateImage(root, "VipWash", new Color(0.35f, 0.12f, 0.45f, 0.07f));
            TournamentUIFactory.StretchRect(vipWash.rectTransform);
            vipWash.rectTransform.anchorMax = new Vector2(1f, 0.55f);

            Sprite bgAsset = TournamentUITheme.Background;
            if (bgAsset)
            {
                Image assetBg = TournamentUIFactory.CreateImage(root, "BgAsset", new Color(1f, 1f, 1f, 0.85f), bgAsset);
                TournamentUIFactory.StretchRect(assetBg.rectTransform);
            }

            Image grad = TournamentUIFactory.CreateImage(root, "BgGrad", new Color(0.06f, 0.2f, 0.14f, 0.55f));
            TournamentUIFactory.StretchRect(grad.rectTransform);

            AddBamboo(root);
            AddGodRays(root);
            AddFog(root);

            Image vignette = TournamentUIFactory.CreateImage(root, "Vignette", new Color(0f, 0f, 0f, 0.48f));
            TournamentUIFactory.StretchRect(vignette.rectTransform);

            AddVipSparkles(root);
            root.gameObject.AddComponent<TournamentFloatingParticles>();
        }

        private static void AddVipSparkles(Transform parent)
        {
            for (int i = 0; i < 8; i++)
            {
                Image s = TournamentUIFactory.CreateImage(parent, "Sparkle", TournamentPremiumTheme.GoldBright, TournamentSpriteFactory.SoftCircle);
                RectTransform rt = s.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.08f + i * 0.11f, 0.72f + (i % 3) * 0.08f);
                float size = TournamentLayoutMetrics.S(10f + (i % 4) * 4f);
                rt.sizeDelta = new Vector2(size, size);
                s.color = new Color(1f, 0.9f, 0.5f, 0.1f + (i % 3) * 0.04f);
            }
        }

        private static void AddBamboo(Transform parent)
        {
            float[] xs = { 0.03f, 0.07f, 0.14f, 0.86f, 0.93f, 0.97f };
            foreach (float x in xs)
            {
                RectTransform stalk = TournamentUIFactory.CreateRect(parent, "Bamboo");
                stalk.anchorMin = stalk.anchorMax = new Vector2(x, 0f);
                stalk.pivot = new Vector2(0.5f, 0f);
                stalk.sizeDelta = new Vector2(28f, 1400f);
                stalk.anchoredPosition = new Vector2(0f, -40f);
                Image img = stalk.gameObject.AddComponent<Image>();
                img.sprite = TournamentSpriteFactory.BambooStripe;
                img.color = new Color(0.15f, 0.35f, 0.22f, 0.55f);
                img.raycastTarget = false;
            }
        }

        private static void AddGodRays(Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                Image ray = TournamentUIFactory.CreateImage(parent, "Ray", new Color(0.95f, 0.85f, 0.45f, 0.04f + i * 0.015f), TournamentSpriteFactory.SoftCircle);
                RectTransform rt = ray.rectTransform;
                float x = 0.25f + i * 0.25f;
                rt.anchorMin = rt.anchorMax = new Vector2(x, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(180f, 900f);
                rt.localRotation = Quaternion.Euler(0f, 0f, -8f + i * 8f);
                rt.anchoredPosition = new Vector2(0f, 60f);
            }
        }

        private static void AddFog(Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                Image fog = TournamentUIFactory.CreateImage(parent, "Fog", new Color(0.7f, 0.88f, 0.78f, 0.04f), TournamentSpriteFactory.SoftCircle);
                RectTransform fr = fog.rectTransform;
                fr.anchorMin = fr.anchorMax = new Vector2(0.3f + i * 0.2f, 0.12f + i * 0.15f);
                fr.sizeDelta = new Vector2(700f, 200f);
                fog.gameObject.AddComponent<TournamentFogDrift>();
            }
        }

        public static Button CreateCircleBackButton(Transform parent, Action onClick)
        {
            RectTransform rt = TournamentUIFactory.CreateRect(parent, "BackButton");
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(TournamentLayoutMetrics.S(18f), -TournamentLayoutMetrics.S(18f));
            rt.sizeDelta = new Vector2(TournamentLayoutMetrics.S(76f), TournamentLayoutMetrics.S(76f));

            Image face = TournamentUIFactory.CreateImage(rt, "Face", Color.white, TournamentSpriteFactory.BackCircle);
            TournamentUIFactory.StretchRect(face.rectTransform);

            Image gloss = TournamentUIFactory.CreateImage(rt, "Gloss", new Color(1f, 1f, 1f, 0.14f), TournamentSpriteFactory.SoftCircle);
            gloss.rectTransform.anchorMin = new Vector2(0.12f, 0.52f);
            gloss.rectTransform.anchorMax = new Vector2(0.88f, 0.96f);
            gloss.rectTransform.offsetMin = gloss.rectTransform.offsetMax = Vector2.zero;

            Text arrow = TournamentUIFactory.CreateText(rt, "Arrow", "←", TournamentLayoutMetrics.Font(36f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(arrow.rectTransform);
            TournamentUIFactory.AddShadow(arrow, new Color(0, 0, 0, 0.55f), new Vector2(2f, -2f));

            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            Button btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit;
            btn.onClick.AddListener(() => onClick?.Invoke());
            rt.gameObject.AddComponent<TournamentPressButton>();
            return btn;
        }

        public static Text CreateWallet(Transform parent, out RectTransform walletRoot)
        {
            walletRoot = TournamentUIFactory.CreateRect(parent, "Wallet");
            walletRoot.anchorMin = new Vector2(1f, 1f);
            walletRoot.anchorMax = new Vector2(1f, 1f);
            walletRoot.pivot = new Vector2(1f, 1f);
            walletRoot.anchoredPosition = new Vector2(-TournamentLayoutMetrics.S(18f), -TournamentLayoutMetrics.S(18f));
            walletRoot.sizeDelta = new Vector2(TournamentLayoutMetrics.S(188f), TournamentLayoutMetrics.S(62f));

            Image bg = TournamentUIFactory.CreateSlicedImage(walletRoot, "Bg", Color.white, TournamentSpriteFactory.Wallet);
            TournamentUIFactory.StretchRect(bg.rectTransform);

            Text coin = TournamentUIFactory.CreateText(walletRoot, "Coin", "🪙", TournamentLayoutMetrics.Font(26f), FontStyle.Normal, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleLeft);
            coin.rectTransform.anchorMin = new Vector2(0f, 0f);
            coin.rectTransform.anchorMax = new Vector2(0f, 1f);
            coin.rectTransform.offsetMin = new Vector2(TournamentLayoutMetrics.S(12f), 0f);
            coin.rectTransform.sizeDelta = new Vector2(TournamentLayoutMetrics.S(32f), 0f);

            Text t = TournamentUIFactory.CreateText(walletRoot, "WalletText", "0", TournamentLayoutMetrics.Font(28f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            t.rectTransform.anchorMin = new Vector2(0.22f, 0f);
            t.rectTransform.anchorMax = new Vector2(1f, 1f);
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = new Vector2(-TournamentLayoutMetrics.S(8f), 0f);
            TournamentUIFactory.AddShadow(t, new Color(0, 0, 0, 0.45f), new Vector2(1f, -1f));
            return t;
        }

        public static void BuildHeaderBlock(Transform parent)
        {
            RectTransform block = TournamentUIFactory.CreateRect(parent, "HeaderBlock");
            block.anchorMin = new Vector2(0f, 1f);
            block.anchorMax = new Vector2(1f, 1f);
            block.pivot = new Vector2(0.5f, 1f);
            block.sizeDelta = new Vector2(0f, TournamentPremiumTheme.HeaderHeight);

            const float logoW = 400f;
            const float logoH = 200f;
            const float topInset = 80f;
            float sLogoW = TournamentLayoutMetrics.S(logoW);
            float sLogoH = TournamentLayoutMetrics.S(logoH);
            float sTop = TournamentLayoutMetrics.S(topInset);

            Sprite logoSprite = TournamentUITheme.Logo;
            float titleY = logoSprite ? -(sTop + sLogoH + 4f) : -(sTop + 40f);

            if (logoSprite)
            {
                Image logo = TournamentUIFactory.CreateImage(block, "Logo", Color.white, logoSprite);
                RectTransform lrt = logo.rectTransform;
                lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1f);
                lrt.pivot = new Vector2(0.5f, 1f);
                lrt.sizeDelta = new Vector2(sLogoW, sLogoH);
                lrt.anchoredPosition = new Vector2(0f, -sTop);
                logo.preserveAspect = true;
            }

            CreateVipBadge(block, -(sTop - TournamentLayoutMetrics.S(8f)));
            Create3DGoldTitle(block, "TOURNAMENTS", titleY - 6f, TournamentLayoutMetrics.Font(54f));

            Text sub = TournamentUIFactory.CreateText(block, "Subtitle", "Compete • Win • Become Champion", TournamentLayoutMetrics.Font(24f), FontStyle.Italic, TournamentPremiumTheme.TextSoft, TextAnchor.MiddleCenter);
            RectTransform srt = sub.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(TournamentLayoutMetrics.S(760f), TournamentLayoutMetrics.S(32f));
            srt.anchoredPosition = new Vector2(0f, titleY - TournamentLayoutMetrics.S(58f));

            CreateGoldDivider(block, titleY - TournamentLayoutMetrics.S(78f));
        }

        private static void CreateVipBadge(Transform parent, float y)
        {
            RectTransform row = TournamentUIFactory.CreateRect(parent, "VipBadge");
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(TournamentLayoutMetrics.S(220f), TournamentLayoutMetrics.S(30f));
            row.anchoredPosition = new Vector2(0f, y);

            Image bg = TournamentUIFactory.CreateSlicedImage(row, "Bg", new Color(0.45f, 0.2f, 0.58f, 0.35f), TournamentSpriteFactory.Badge);
            TournamentUIFactory.StretchRect(bg.rectTransform);

            Text t = TournamentUIFactory.CreateText(row, "Label", "👑 VIP LOUNGE", TournamentLayoutMetrics.Font(16f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(t.rectTransform);
        }

        public static void CreateGoldDivider(Transform parent, float y)
        {
            RectTransform row = TournamentUIFactory.CreateRect(parent, "Divider");
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(TournamentLayoutMetrics.S(520f), TournamentLayoutMetrics.S(4f));
            row.anchoredPosition = new Vector2(0f, y);

            Image line = TournamentUIFactory.CreateImage(row, "Line", TournamentPremiumTheme.Gold);
            line.rectTransform.anchorMin = new Vector2(0.12f, 0.5f);
            line.rectTransform.anchorMax = new Vector2(0.88f, 0.5f);
            line.rectTransform.sizeDelta = new Vector2(0f, TournamentLayoutMetrics.S(2f));
        }

        public static void CreateSectionTitle(Transform parent, string text)
        {
            RectTransform row = TournamentUIFactory.CreateRect(parent, "Section");
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = TournamentPremiumTheme.SectionTitleHeight;

            Image left = TournamentUIFactory.CreateImage(row, "LineL", TournamentPremiumTheme.Gold);
            left.rectTransform.anchorMin = new Vector2(0.04f, 0.5f);
            left.rectTransform.anchorMax = new Vector2(0.22f, 0.5f);
            left.rectTransform.sizeDelta = new Vector2(0f, TournamentLayoutMetrics.S(2f));

            Image right = TournamentUIFactory.CreateImage(row, "LineR", TournamentPremiumTheme.Gold);
            right.rectTransform.anchorMin = new Vector2(0.78f, 0.5f);
            right.rectTransform.anchorMax = new Vector2(0.96f, 0.5f);
            right.rectTransform.sizeDelta = new Vector2(0f, TournamentLayoutMetrics.S(2f));

            Text t = TournamentUIFactory.CreateText(row, "Label", text, TournamentLayoutMetrics.Font(32f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(t.rectTransform);
            TournamentUIFactory.AddGoldOutline(t);
        }

        public static RectTransform CreateCardShell(Transform parent)
        {
            Image shadow = TournamentUIFactory.CreateSlicedImage(parent, "Shadow", new Color(0f, 0f, 0f, 0.5f), TournamentSpriteFactory.CardBackground);
            RectTransform srt = shadow.rectTransform;
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(5f, -10f);
            srt.offsetMax = new Vector2(5f, -10f);
            shadow.transform.SetAsFirstSibling();

            Image panel = TournamentUIFactory.CreateSlicedImage(parent, "CardPanel", Color.white, TournamentSpriteFactory.CardBackground);
            RectTransform prt = panel.rectTransform;
            TournamentUIFactory.StretchRect(prt);

            Image vipTrim = TournamentUIFactory.CreateSlicedImage(panel.transform, "VipTrim", new Color(1f, 0.9f, 0.45f, 0.35f), TournamentSpriteFactory.GoldFrame);
            TournamentUIFactory.StretchRect(vipTrim.rectTransform);
            vipTrim.rectTransform.offsetMin = new Vector2(4f, 4f);
            vipTrim.rectTransform.offsetMax = new Vector2(-4f, -4f);

            Image innerGlow = TournamentUIFactory.CreateSlicedImage(panel.transform, "InnerGlow", new Color(0.22f, 0.55f, 0.38f, 0.12f), TournamentSpriteFactory.CardBackground);
            TournamentUIFactory.StretchRect(innerGlow.rectTransform);
            innerGlow.rectTransform.offsetMin = new Vector2(10f, 10f);
            innerGlow.rectTransform.offsetMax = new Vector2(-10f, -10f);

            Image glass = TournamentUIFactory.CreateImage(panel.transform, "Glass", Color.white, TournamentSpriteFactory.GlassSheen);
            RectTransform gl = glass.rectTransform;
            gl.anchorMin = new Vector2(0f, 0.42f);
            gl.anchorMax = new Vector2(1f, 1f);
            gl.offsetMin = new Vector2(12f, 0f);
            gl.offsetMax = new Vector2(-12f, -8f);

            Image gloss = TournamentUIFactory.CreateImage(panel.transform, "TopGloss", new Color(1f, 1f, 1f, 0.06f));
            gloss.rectTransform.anchorMin = new Vector2(0f, 1f);
            gloss.rectTransform.anchorMax = new Vector2(1f, 1f);
            gloss.rectTransform.pivot = new Vector2(0.5f, 1f);
            gloss.rectTransform.sizeDelta = new Vector2(0f, 36f);

            panel.gameObject.AddComponent<TournamentCardGlow>();
            panel.gameObject.AddComponent<TournamentGoldShine>();
            return prt;
        }

        public static RectTransform CreateHexIcon(Transform parent, string emoji, float size)
        {
            RectTransform frame = TournamentUIFactory.CreateRect(parent, "HexIcon");
            frame.sizeDelta = new Vector2(size, size);

            Image hex = TournamentUIFactory.CreateImage(frame, "Hex", Color.white, TournamentSpriteFactory.HexFrame);
            TournamentUIFactory.StretchRect(hex.rectTransform);

            Text icon = TournamentUIFactory.CreateText(frame, "Emoji", emoji, Mathf.RoundToInt(size * 0.44f), FontStyle.Normal, TournamentPremiumTheme.TextWhite, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(icon.rectTransform);
            TournamentUIFactory.AddShadow(icon, new Color(0, 0, 0, 0.55f), new Vector2(2f, -2f));
            return frame;
        }

        public static Button CreateJoinButton(Transform parent, Action onClick, int labelFontSize = 0)
        {
            int fontSize = labelFontSize > 0 ? labelFontSize : TournamentPremiumTheme.JoinButtonFontSize;
            RectTransform rt = TournamentUIFactory.CreateRect(parent, "JoinButton");

            Sprite assetBtn = TournamentUITheme.JoinButtonNormal ?? TournamentUITheme.LongButtonNormal ?? TournamentUITheme.ButtonNormal;
            Sprite assetHover = TournamentUITheme.JoinButtonHover ?? TournamentUITheme.LongButtonHover ?? TournamentUITheme.ButtonPressed ?? TournamentUITheme.ButtonHover;
            Sprite fallbackBtn = TournamentSpriteFactory.ButtonGreen;
            Image shadow = TournamentUIFactory.CreateSlicedImage(rt, "Shadow", new Color(0f, 0f, 0f, 0.45f), fallbackBtn);
            RectTransform sh = shadow.rectTransform;
            sh.anchorMin = Vector2.zero;
            sh.anchorMax = Vector2.one;
            sh.offsetMin = new Vector2(2f, -8f);
            sh.offsetMax = new Vector2(2f, -3f);

            Image bg = TournamentUIFactory.CreateSlicedImage(rt, "Bg", Color.white, assetBtn ? assetBtn : fallbackBtn);
            TournamentUIFactory.StretchRect(bg.rectTransform);
            bg.raycastTarget = true;

            Image gloss = TournamentUIFactory.CreateSlicedImage(rt, "Gloss", new Color(1f, 1f, 1f, 0.2f), assetBtn ? assetBtn : fallbackBtn);
            gloss.raycastTarget = false;
            gloss.rectTransform.anchorMin = new Vector2(0.06f, 0.58f);
            gloss.rectTransform.anchorMax = new Vector2(0.94f, 0.94f);
            gloss.rectTransform.offsetMin = gloss.rectTransform.offsetMax = Vector2.zero;

            Button btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            if (assetBtn)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                SpriteState st = btn.spriteState;
                st.highlightedSprite = assetHover ? assetHover : assetBtn;
                st.pressedSprite = assetHover ? assetHover : assetBtn;
                btn.spriteState = st;
            }
            btn.onClick.AddListener(() => onClick?.Invoke());
            rt.gameObject.AddComponent<TournamentPressButton>();
            rt.gameObject.AddComponent<TournamentJoinButtonShine>();
            rt.gameObject.AddComponent<TournamentButtonGlow>();

            Text label = TournamentUIFactory.CreateText(rt, "Label", "JOIN", fontSize, FontStyle.Bold, TournamentPremiumTheme.TextWhite, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(label.rectTransform);
            TournamentUIFactory.AddShadow(label, new Color(0, 0, 0, 0.55f), new Vector2(1.5f, -1.5f));
            return btn;
        }

        public static void LayoutJoinColumn(RectTransform rightColumn, Button joinButton, string status)
        {
            RectTransform joinRt = joinButton.GetComponent<RectTransform>();
            joinRt.anchorMin = new Vector2(0.06f, 0.5f);
            joinRt.anchorMax = new Vector2(0.94f, 0.5f);
            joinRt.pivot = new Vector2(0.5f, 0.5f);
            joinRt.sizeDelta = new Vector2(0f, TournamentPremiumTheme.JoinButtonHeight);
            joinRt.anchoredPosition = new Vector2(0f, TournamentLayoutMetrics.S(22f));

            CreateStatusBadge(rightColumn, status);
        }

        public static void CreateStatusBadge(Transform parent, string status)
        {
            Color c = GetStatusColor(status);
            Image bg = TournamentUIFactory.CreateSlicedImage(parent, "Badge", c, TournamentSpriteFactory.Badge);
            RectTransform rt = bg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, TournamentPremiumTheme.BadgeHeight);
            rt.anchoredPosition = new Vector2(0f, 6f);

            Image bloom = TournamentUIFactory.CreateImage(bg.transform, "Bloom", new Color(1f, 1f, 1f, 0.12f), TournamentSpriteFactory.SoftCircle);
            TournamentUIFactory.StretchRect(bloom.rectTransform);

            Text label = TournamentUIFactory.CreateText(bg.transform, "Label", status, TournamentLayoutMetrics.Font(17f), FontStyle.Bold, TournamentPremiumTheme.TextWhite, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(label.rectTransform);
            TournamentUIFactory.AddShadow(label, new Color(0, 0, 0, 0.4f), new Vector2(1f, -1f));
        }

        public static void CreateTypeButton(Transform parent, string icon, string label)
        {
            RectTransform item = TournamentUIFactory.CreateRect(parent, "TypeBtn");

            Image shadow = TournamentUIFactory.CreateSlicedImage(item, "Shadow", new Color(0, 0, 0, 0.42f), TournamentSpriteFactory.CardBackground);
            TournamentUIFactory.StretchRect(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(3f, -5f);
            shadow.rectTransform.offsetMax = new Vector2(-3f, -7f);

            Image panel = TournamentUIFactory.CreateSlicedImage(item, "Panel", Color.white, TournamentSpriteFactory.CardBackground);
            RectTransform prt = panel.rectTransform;
            TournamentUIFactory.StretchRect(prt);
            prt.offsetMin = new Vector2(3f, 3f);
            prt.offsetMax = new Vector2(-3f, -3f);

            Image vipEdge = TournamentUIFactory.CreateSlicedImage(panel.transform, "VipEdge", new Color(1f, 0.88f, 0.4f, 0.2f), TournamentSpriteFactory.GoldFrame);
            TournamentUIFactory.StretchRect(vipEdge.rectTransform);
            vipEdge.rectTransform.offsetMin = new Vector2(5f, 5f);
            vipEdge.rectTransform.offsetMax = new Vector2(-5f, -5f);

            Image glass = TournamentUIFactory.CreateImage(panel.transform, "Glass", Color.white, TournamentSpriteFactory.GlassSheen);
            glass.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            glass.rectTransform.anchorMax = new Vector2(1f, 1f);
            glass.rectTransform.offsetMin = new Vector2(8f, 0f);
            glass.rectTransform.offsetMax = new Vector2(-8f, -6f);

            Image hit = item.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            Button btn = item.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit;
            btn.transition = Selectable.Transition.None;
            item.gameObject.AddComponent<TournamentPressButton>();
            item.gameObject.AddComponent<TournamentCardGlow>();

            RectTransform hex = CreateHexIcon(panel.transform, icon, TournamentLayoutMetrics.S(44f));
            hex.anchorMin = hex.anchorMax = new Vector2(0.5f, 0.68f);
            hex.anchoredPosition = Vector2.zero;

            Text t = TournamentUIFactory.CreateText(panel.transform, "Label", label, TournamentLayoutMetrics.Font(13f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            t.rectTransform.anchorMin = new Vector2(0.04f, 0.04f);
            t.rectTransform.anchorMax = new Vector2(0.96f, 0.3f);
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        }

        public static void CreateInfoCard(Transform parent, string icon, string title, string body)
        {
            RectTransform item = TournamentUIFactory.CreateRect(parent, "InfoCard");
            LayoutElement le = item.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = TournamentPremiumTheme.InfoCardHeight;

            Image shadow = TournamentUIFactory.CreateSlicedImage(item, "Shadow", new Color(0, 0, 0, 0.38f), TournamentSpriteFactory.CardBackground);
            TournamentUIFactory.StretchRect(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(4f, -4f);
            shadow.rectTransform.offsetMax = new Vector2(-4f, -8f);

            Image panel = TournamentUIFactory.CreateSlicedImage(item, "Panel", Color.white, TournamentSpriteFactory.CardBackground);
            TournamentUIFactory.StretchRect(panel.rectTransform);
            panel.rectTransform.offsetMin = new Vector2(4f, 4f);
            panel.rectTransform.offsetMax = new Vector2(-4f, -4f);

            Image vipEdge = TournamentUIFactory.CreateSlicedImage(panel.transform, "VipEdge", new Color(1f, 0.88f, 0.4f, 0.18f), TournamentSpriteFactory.GoldFrame);
            TournamentUIFactory.StretchRect(vipEdge.rectTransform);
            vipEdge.rectTransform.offsetMin = new Vector2(6f, 6f);
            vipEdge.rectTransform.offsetMax = new Vector2(-6f, -6f);

            RectTransform hex = CreateHexIcon(panel.transform, icon, TournamentLayoutMetrics.S(64f));
            hex.anchorMin = new Vector2(0f, 0.5f);
            hex.anchorMax = new Vector2(0f, 0.5f);
            hex.pivot = new Vector2(0f, 0.5f);
            hex.anchoredPosition = new Vector2(TournamentLayoutMetrics.S(16f), 0f);

            Text titleT = TournamentUIFactory.CreateText(panel.transform, "Title", title, TournamentLayoutMetrics.Font(26f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.UpperLeft);
            titleT.rectTransform.anchorMin = new Vector2(0.24f, 0.52f);
            titleT.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
            titleT.rectTransform.offsetMin = titleT.rectTransform.offsetMax = Vector2.zero;

            Text bodyT = TournamentUIFactory.CreateText(panel.transform, "Body", body, TournamentLayoutMetrics.Font(20f), FontStyle.Normal, TournamentPremiumTheme.TextMuted, TextAnchor.UpperLeft);
            bodyT.rectTransform.anchorMin = new Vector2(0.24f, 0.08f);
            bodyT.rectTransform.anchorMax = new Vector2(0.96f, 0.5f);
            bodyT.rectTransform.offsetMin = bodyT.rectTransform.offsetMax = Vector2.zero;
        }

        public static void CreateInfoCardsRow(Transform parent, (string icon, string title, string body)[] cards)
        {
            RectTransform row = TournamentUIFactory.CreateRect(parent, "InfoRow");
            LayoutElement rowLe = row.gameObject.AddComponent<LayoutElement>();
            rowLe.preferredHeight = TournamentLayoutMetrics.S(220f);

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = TournamentLayoutMetrics.S(10f);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach ((string icon, string title, string body) in cards)
                CreateInfoCardCell(row, icon, title, body);
        }

        private static void CreateInfoCardCell(Transform parent, string icon, string title, string body)
        {
            RectTransform item = TournamentUIFactory.CreateRect(parent, "InfoCell");
            LayoutElement le = item.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            Image panel = TournamentUIFactory.CreateSlicedImage(item, "Panel", Color.white, TournamentSpriteFactory.CardBackground);
            TournamentUIFactory.StretchRect(panel.rectTransform);

            Image vipEdge = TournamentUIFactory.CreateSlicedImage(panel.transform, "VipEdge", new Color(1f, 0.88f, 0.4f, 0.2f), TournamentSpriteFactory.GoldFrame);
            TournamentUIFactory.StretchRect(vipEdge.rectTransform);
            vipEdge.rectTransform.offsetMin = new Vector2(4f, 4f);
            vipEdge.rectTransform.offsetMax = new Vector2(-4f, -4f);

            RectTransform hex = CreateHexIcon(panel.transform, icon, TournamentLayoutMetrics.S(48f));
            hex.anchorMin = hex.anchorMax = new Vector2(0.5f, 0.78f);
            hex.anchoredPosition = Vector2.zero;

            Text titleT = TournamentUIFactory.CreateText(panel.transform, "Title", title, TournamentLayoutMetrics.Font(16f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.UpperCenter);
            titleT.rectTransform.anchorMin = new Vector2(0.06f, 0.42f);
            titleT.rectTransform.anchorMax = new Vector2(0.94f, 0.72f);
            titleT.rectTransform.offsetMin = titleT.rectTransform.offsetMax = Vector2.zero;

            Text bodyT = TournamentUIFactory.CreateText(panel.transform, "Body", body, TournamentLayoutMetrics.Font(14f), FontStyle.Normal, TournamentPremiumTheme.TextMuted, TextAnchor.UpperCenter);
            bodyT.rectTransform.anchorMin = new Vector2(0.06f, 0.06f);
            bodyT.rectTransform.anchorMax = new Vector2(0.94f, 0.4f);
            bodyT.rectTransform.offsetMin = bodyT.rectTransform.offsetMax = Vector2.zero;
        }

        public static void CreateDialogPanel(Transform parent)
        {
            Image bg = TournamentUIFactory.CreateSlicedImage(parent, "PanelBg", Color.white, TournamentSpriteFactory.CardBackground);
            TournamentUIFactory.StretchRect(bg.rectTransform);

            Image glass = TournamentUIFactory.CreateImage(bg.transform, "Glass", Color.white, TournamentSpriteFactory.GlassSheen);
            glass.rectTransform.anchorMin = new Vector2(0f, 0.55f);
            glass.rectTransform.anchorMax = new Vector2(1f, 1f);
            glass.rectTransform.offsetMin = new Vector2(12f, 0f);
            glass.rectTransform.offsetMax = new Vector2(-12f, -10f);
        }

        public static Color GetStatusColor(string status)
        {
            if (string.IsNullOrEmpty(status)) return TournamentPremiumTheme.BadgeOpen;
            switch (status.Trim().ToUpperInvariant())
            {
                case "OPEN": return TournamentPremiumTheme.BadgeOpen;
                case "FILLING": return TournamentPremiumTheme.BadgeFilling;
                case "STARTING SOON": return TournamentPremiumTheme.BadgeStartingSoon;
                case "FULL": return TournamentPremiumTheme.BadgeFull;
                default: return TournamentPremiumTheme.BadgeOpen;
            }
        }

        private static void Create3DGoldTitle(Transform parent, string text, float y, float fontSize)
        {
            float titleW = TournamentLayoutMetrics.S(900f);
            float titleH = fontSize + TournamentLayoutMetrics.S(14f);

            Text shadow = TournamentUIFactory.CreateText(parent, "TitleShadow", text, Mathf.RoundToInt(fontSize), FontStyle.Bold, new Color(0, 0, 0, 0.65f), TextAnchor.MiddleCenter);
            shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shadow.rectTransform.pivot = new Vector2(0.5f, 1f);
            shadow.rectTransform.sizeDelta = new Vector2(titleW, titleH);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, y - 5f);

            Text depth = TournamentUIFactory.CreateText(parent, "TitleDepth", text, Mathf.RoundToInt(fontSize), FontStyle.Bold, new Color(0.5f, 0.38f, 0.1f, 1f), TextAnchor.MiddleCenter);
            depth.rectTransform.anchorMin = depth.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            depth.rectTransform.pivot = new Vector2(0.5f, 1f);
            depth.rectTransform.sizeDelta = new Vector2(titleW, titleH);
            depth.rectTransform.anchoredPosition = new Vector2(2f, y - 2f);

            Text main = TournamentUIFactory.CreateText(parent, "Title", text, Mathf.RoundToInt(fontSize), FontStyle.Bold, TournamentPremiumTheme.Gold, TextAnchor.MiddleCenter);
            main.rectTransform.anchorMin = main.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            main.rectTransform.pivot = new Vector2(0.5f, 1f);
            main.rectTransform.sizeDelta = new Vector2(titleW, titleH);
            main.rectTransform.anchoredPosition = new Vector2(0f, y);
            TournamentUIFactory.AddGoldOutline(main);
        }
    }
}
