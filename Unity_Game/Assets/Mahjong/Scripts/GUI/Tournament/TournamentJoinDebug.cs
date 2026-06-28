using System;
using System.Collections.Generic;
using System.Text;
using Mkey;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Debug logging for the 1 vs 1 Duel JOIN button investigation.
    /// </summary>
    public static class TournamentJoinDebug
    {
        public const string FirstJoinId = "duel_1v1";
        public const string FirstJoinObjectName = "Join_duel_1v1";
        private const string Tag = "[TournamentJoinDebug]";

        public static bool IsFirstJoin(TournamentDefinition tournament) =>
            tournament != null && tournament.id == FirstJoinId;

        public static void Log(string message) => Debug.Log($"{Tag} {message}");

        public static void LogWarning(string message) => Debug.LogWarning($"{Tag} {message}");

        public static void LogError(string message) => Debug.LogError($"{Tag} {message}");

        public static void LogFirstJoinButtonSetup(Transform buttonTransform, Rect joinRect)
        {
            if (!buttonTransform || buttonTransform.name != FirstJoinObjectName) return;

            Log("=== 1 vs 1 JOIN BUTTON SETUP ===");
            Log($"Join rect (ref px): x={joinRect.x}, y={joinRect.y}, w={joinRect.width}, h={joinRect.height}");
            LogUiState(buttonTransform.gameObject, "Setup");
            LogRaycastStackAtButtonCenter(buttonTransform as RectTransform, "Setup");
        }

        public static void LogPointerDown(GameObject target, string source)
        {
            if (!IsFirstJoinObject(target)) return;
            Log($"1 vs 1 JOIN button pointer down ({source}) — button IS receiving input");
            LogUiState(target, "PointerDown");
        }

        public static void LogPointerClick(GameObject target, string source)
        {
            if (!IsFirstJoinObject(target)) return;
            Log("1 vs 1 JOIN button clicked");
            Log($"Click received via: {source}");
            Log("Is the Button receiving the click? YES");
        }

        public static void LogButtonOnClickExecuted(string buttonName)
        {
            if (buttonName != FirstJoinObjectName) return;
            Log("Button OnClick event executed");
        }

        public static void LogOnJoinTournamentEnter(TournamentDefinition tournament)
        {
            if (!IsFirstJoin(tournament)) return;

            Log("=== OnJoinTournament START (1 vs 1 Duel) ===");
            Log($"TournamentDefinition null? {(tournament == null ? "YES" : "NO")}");
            if (tournament == null)
            {
                LogError("STOP: TournamentDefinition is null");
                return;
            }

            Log($"Tournament ID: {tournament.id}");
            Log($"Tournament Name: {tournament.displayName}");
            Log($"Entry Fee: {tournament.entryFee:N0}");
            Log($"CoinsHolder.Instance exists? {(CoinsHolder.Instance ? "YES" : "NO")}");

            int balance = CoinsHolder.Instance ? CoinsHolder.Count : 0;
            Log($"Current Coin Balance: {balance:N0}");
            bool pass = balance >= tournament.entryFee;
            Log($"Balance check passing? {(pass ? "YES" : "NO")}");
            if (!pass)
                LogWarning("STOP: Insufficient coins — will open insufficient-coins dialog");
        }

        public static void LogDialogOpening(string context, string title)
        {
            Log($"TournamentDialog opening ({context}) — title: \"{title}\"");
        }

        public static void LogDialogFailed(string context, string reason)
        {
            LogError($"TournamentDialog NOT opening ({context}) — {reason}");
        }

        public static void LogConfirmJoinEnter(TournamentDefinition tournament)
        {
            if (!IsFirstJoin(tournament)) return;
            Log("=== ConfirmJoin START (1 vs 1 Duel) ===");
        }

        public static void LogConfirmJoinStop(TournamentDefinition tournament, string reason)
        {
            if (!IsFirstJoin(tournament)) return;
            LogWarning($"STOP ConfirmJoin: {reason}");
        }

        public static void LogWaitingRoomOpening(TournamentDefinition tournament)
        {
            if (!IsFirstJoin(tournament)) return;
            Log("WaitingRoom opening for 1 vs 1 Duel");
        }

        public static void LogException(string context, Exception ex)
        {
            LogError($"EXCEPTION in {context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }

        public static void LogClickBlocked(Vector2 screenPos, Transform expectedButton, string reason, GameObject topBlocker)
        {
            LogWarning("JOIN BUTTON CLICK BLOCKED");
            LogWarning($"Reason: {reason}");
            LogWarning($"Screen position: {screenPos}");
            if (expectedButton)
                LogWarning($"Expected button: {expectedButton.name}");
            if (topBlocker)
            {
                LogWarning($"Top raycast hit: {GetTransformPath(topBlocker.transform)}");
                LogUiState(topBlocker, "Blocker");
            }

            LogRaycastAll(screenPos);
        }

        public static void LogUiState(GameObject target, string context)
        {
            if (!target) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"--- UI state ({context}) for {GetTransformPath(target.transform)} ---");

            Button button = target.GetComponent<Button>();
            if (button)
            {
                sb.AppendLine($"Button.interactable: {button.interactable}");
                sb.AppendLine($"Button.enabled: {button.enabled}");
                sb.AppendLine($"Button.onClick listener count: {button.onClick.GetPersistentEventCount()} + runtime listeners");
            }
            else
            {
                sb.AppendLine("Button component: MISSING");
            }

            Image image = target.GetComponent<Image>();
            if (image)
                sb.AppendLine($"Image.raycastTarget: {image.raycastTarget}");

            CanvasGroup cg = target.GetComponent<CanvasGroup>();
            if (cg)
            {
                sb.AppendLine($"CanvasGroup.blocksRaycasts: {cg.blocksRaycasts}");
                sb.AppendLine($"CanvasGroup.interactable: {cg.interactable}");
                sb.AppendLine($"CanvasGroup.alpha: {cg.alpha}");
            }

            sb.AppendLine($"GameObject.activeSelf: {target.activeSelf}");
            sb.AppendLine($"GameObject.activeInHierarchy: {target.activeInHierarchy}");

            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas)
            {
                sb.AppendLine($"Canvas.enabled: {canvas.enabled}");
                sb.AppendLine($"Canvas.renderMode: {canvas.renderMode}");
                sb.AppendLine($"Canvas.sortingOrder: {canvas.sortingOrder}");
                GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
                sb.AppendLine($"GraphicRaycaster: {(gr ? (gr.enabled ? "enabled" : "disabled") : "missing")}");
            }

            EventSystem es = EventSystem.current;
            sb.AppendLine($"EventSystem.active: {(es && es.isActiveAndEnabled ? "YES" : "NO")}");

            ScrollRect scroll = target.GetComponentInParent<ScrollRect>();
            sb.AppendLine($"Parent ScrollRect: {(scroll ? scroll.name : "none")}");

            Log(sb.ToString());
        }

        public static void LogRaycastStackAtButtonCenter(RectTransform button, string context)
        {
            if (!button) return;
            Vector3[] corners = new Vector3[4];
            button.GetWorldCorners(corners);
            Vector2 center = (corners[0] + corners[2]) * 0.5f;
            Camera cam = GetEventCamera(button);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, center);
            Log($"Raycast stack at button center ({context}), screen {screen}:");
            LogRaycastAll(screen);
        }

        public static void LogRaycastAll(Vector2 screenPos)
        {
            if (!EventSystem.current)
            {
                LogError("EventSystem.current is null — cannot raycast");
                return;
            }

            PointerEventData ped = new PointerEventData(EventSystem.current) { position = screenPos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            if (results.Count == 0)
            {
                Log("RaycastAll: no UI hits");
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                RaycastResult r = results[i];
                Image img = r.gameObject.GetComponent<Image>();
                string raycast = img ? $"Image.raycastTarget={img.raycastTarget}" : "no Image";
                Log($"  [{i}] {GetTransformPath(r.gameObject.transform)} | {raycast} | depth={r.depth}");
            }
        }

        private static bool IsFirstJoinObject(GameObject go) =>
            go && go.name == FirstJoinObjectName;

        private static Camera GetEventCamera(RectTransform rt)
        {
            Canvas canvas = rt ? rt.GetComponentInParent<Canvas>() : null;
            if (!canvas) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private static string GetTransformPath(Transform t)
        {
            if (!t) return "null";
            string path = t.name;
            while (t.parent)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
