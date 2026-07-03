using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    public static class TournamentUITheme
    {
        public static readonly Color GreenCameraBg = new Color(0.2784314f, 0.3882353f, 0.3764706f, 1f);
        public static readonly Color Gold = new Color(0.831f, 0.686f, 0.216f, 1f);
        public static readonly Color OrangeFallback = new Color(0.92f, 0.48f, 0.12f, 1f);
        public static readonly Color ButtonLabel = Color.white;

        private static Font _font;
        private static TournamentVisualsData _visuals;
        private static Sprite _pageDesign;

        public static Font Font
        {
            get
            {
                if (!_font) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        private static TournamentVisualsData Visuals
        {
            get
            {
                if (!_visuals) _visuals = Resources.Load<TournamentVisualsData>("Tournament/TournamentVisualsData");
                return _visuals;
            }
        }

        public static Sprite Logo => Visuals && Visuals.logo ? Visuals.logo : null;
        public static Sprite Background => Visuals && Visuals.background ? Visuals.background : null;
        public static Sprite PageDesign
        {
            get
            {
                if (_pageDesign) return _pageDesign;
                _pageDesign = Resources.Load<Sprite>("Tournament/turnamant1");
                if (_pageDesign) return _pageDesign;
                Sprite[] sprites = Resources.LoadAll<Sprite>("Tournament/turnamant1");
                if (sprites != null && sprites.Length > 0) _pageDesign = sprites[0];
                if (_pageDesign) return _pageDesign;
                _pageDesign = Resources.Load<Sprite>("Tournament/turnamant");
                if (_pageDesign) return _pageDesign;
                sprites = Resources.LoadAll<Sprite>("Tournament/turnamant");
                if (sprites != null && sprites.Length > 0) _pageDesign = sprites[0];
                return _pageDesign;
            }
        }
        public static Sprite JoinButtonNormal => Visuals ? Visuals.joinButtonNormal : null;
        public static Sprite JoinButtonHover => Visuals ? Visuals.joinButtonHover : null;
        public static Sprite LongButtonNormal => Visuals ? Visuals.longButtonNormal : null;
        public static Sprite LongButtonHover => Visuals ? Visuals.longButtonHover : null;
        public static Sprite ButtonNormal => Visuals ? Visuals.buttonNormal : null;
        public static Sprite ButtonPressed => Visuals ? Visuals.buttonPressed : null;
        public static Sprite ButtonHover => Visuals ? Visuals.buttonHover : null;
        public static Sprite MapPlayButtonNormal => Visuals ? Visuals.mapPlayButtonNormal : null;
        public static Sprite MapPlayButtonPressed => Visuals ? Visuals.mapPlayButtonPressed : null;
    }

    public static class TournamentUIFactory
    {
        public static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            RectTransform rt = CreateRect(parent, name);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Image CreateSlicedImage(Transform parent, string name, Color color, Sprite sprite, bool raycast = false)
        {
            Image img = CreateImage(parent, name, color, sprite, raycast);
            img.type = Image.Type.Sliced;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            RectTransform rt = CreateRect(parent, name);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = TournamentUITheme.Font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static void AddGoldOutline(Graphic graphic)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = TournamentPremiumTheme.Gold;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        public static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        public static Button CreateGameButton(Transform parent, string label, Vector2 size, Action onClick, bool longStyle = false)
        {
            RectTransform rt = CreateRect(parent, "Button_" + label);
            rt.sizeDelta = size;
            Image image = rt.gameObject.AddComponent<Image>();
            Sprite normal = longStyle ? TournamentUITheme.LongButtonNormal : TournamentUITheme.ButtonNormal;
            if (normal) { image.sprite = normal; image.color = Color.white; }
            else image.color = TournamentUITheme.OrangeFallback;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            Text text = CreateText(rt, "Label", label, longStyle ? 32 : 28, FontStyle.Bold, TournamentUITheme.ButtonLabel, TextAnchor.MiddleCenter);
            StretchRect(text.rectTransform);
            return button;
        }

        public static void CreateInvisibleButton(Transform parent, string name, Rect rect, Action onClick)
        {
            RectTransform rt = CreateRect(parent, name);
            TournamentPngLayout.PlaceFromTopLeftAnchored(rt, rect);
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.sprite = TournamentSpriteFactory.SoftCircle;
            hit.type = Image.Type.Simple;
            hit.color = new Color(1f, 1f, 1f, 0.04f);
            hit.raycastTarget = true;

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        public static void CreateJoinButton(Transform parent, TournamentDefinition tournament, Rect rect, Action<TournamentDefinition> onJoin)
        {
            if (parent.name == "HitAreas" && !parent.GetComponent<TournamentHitAreasBootstrap>())
                parent.gameObject.AddComponent<TournamentHitAreasBootstrap>();

            if (parent.name == "HitAreas" && !parent.GetComponent<TournamentFirstJoinClickProbe>())
                parent.gameObject.AddComponent<TournamentFirstJoinClickProbe>();

            string buttonName = tournament != null ? "Join_" + tournament.id : "Join_unknown";
            RectTransform rt = CreateRect(parent, buttonName);
            TournamentPngLayout.PlaceFromTopLeftAnchored(rt, rect);

            Image hit = rt.gameObject.AddComponent<Image>();
            hit.sprite = TournamentSpriteFactory.SoftCircle;
            hit.type = Image.Type.Simple;
            hit.color = new Color(1f, 1f, 1f, 0.04f);
            hit.raycastTarget = true;

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            bool isFull = tournament != null &&
                string.Equals(tournament.statusLabel, "FULL", StringComparison.OrdinalIgnoreCase);
            button.interactable = !isFull;

            rt.gameObject.AddComponent<TournamentJoinHitArea>();

            TournamentJoinButton joinButton = rt.gameObject.AddComponent<TournamentJoinButton>();
            joinButton.Bind(tournament, onJoin);
            CreateJoinButtonLabel(rt, isFull);

            if (tournament != null && tournament.id == TournamentJoinDebug.FirstJoinId)
                TournamentJoinDebug.LogFirstJoinButtonSetup(rt, rect);
        }

        private static void CreateJoinButtonLabel(RectTransform buttonRt, bool isFull)
        {
            Text label = CreateText(
                buttonRt,
                "Label",
                isFull ? "FULL" : "JOIN",
                TournamentPngLayout.OverlayFont(20f),
                FontStyle.Bold,
                Color.white,
                TextAnchor.MiddleCenter);
            StretchRect(label.rectTransform);
            label.raycastTarget = false;
            AddShadow(label, new Color(0f, 0f, 0f, 0.55f), new Vector2(1f, -1f));
        }

        public static Text CreateWalletBalance(Transform parent)
        {
            RectTransform panel = CreateRect(parent, "WalletBalance");
            TournamentPngLayout.PlaceFromTopLeftAnchored(panel, TournamentPngLayout.Wallet);

            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            int fontSize = Mathf.Max(16, TournamentPngLayout.OverlayFont(20f));
            Text amount = CreateText(
                panel,
                "WalletText",
                string.Empty,
                fontSize,
                FontStyle.Bold,
                Color.white,
                TextAnchor.MiddleCenter);
            StretchRect(amount.rectTransform);
            amount.alignByGeometry = true;
            amount.horizontalOverflow = HorizontalWrapMode.Overflow;
            amount.verticalOverflow = VerticalWrapMode.Truncate;
            amount.resizeTextForBestFit = true;
            amount.resizeTextMinSize = 12;
            amount.resizeTextMaxSize = fontSize;
            amount.raycastTarget = false;
            return amount;
        }

        public static RectTransform CreateDepositButton(Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform panel = CreateRect(parent, "DepositButton");
            TournamentPngLayout.PlaceFromTopLeftAnchored(panel, TournamentPngLayout.Deposit);

            Image hit = CreateImage(panel, "Hit", new Color(1f, 1f, 1f, 0.02f), TournamentSpriteFactory.SoftCircle, true);
            StretchRect(hit.rectTransform);

            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.onClick.AddListener(onClick);
            return panel;
        }

        public static Text CreateOverlayText(Transform parent, string name, Rect rect, string content, int fontSize, FontStyle style, Color color, TextAnchor anchor)
        {
            RectTransform rt = CreateRect(parent, name);
            TournamentPngLayout.PlaceFromTopLeftAnchored(rt, rect);
            CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Text text = rt.gameObject.AddComponent<Text>();
            text.font = TournamentUITheme.Font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
