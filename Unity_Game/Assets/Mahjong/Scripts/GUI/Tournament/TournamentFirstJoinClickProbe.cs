using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Detects taps on the 1 vs 1 JOIN rect that never reach Join_duel_1v1.
    /// </summary>
    public class TournamentFirstJoinClickProbe : MonoBehaviour
    {
        private RectTransform joinButton;
        private Camera eventCamera;

        private void Start()
        {
            Transform join = transform.Find(TournamentJoinDebug.FirstJoinObjectName);
            if (!join)
            {
                TournamentJoinDebug.LogWarning("TournamentFirstJoinClickProbe: Join_duel_1v1 not found");
                enabled = false;
                return;
            }

            joinButton = join as RectTransform;
            Canvas canvas = joinButton.GetComponentInParent<Canvas>();
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera;
        }

        private void Update()
        {
            if (!joinButton || !WasPointerPressedThisFrame(out Vector2 screenPos)) return;

            bool insideJoinRect = RectTransformUtility.RectangleContainsScreenPoint(joinButton, screenPos, eventCamera);
            if (!insideJoinRect) return;

            if (!EventSystem.current)
            {
                TournamentJoinDebug.LogClickBlocked(screenPos, joinButton, "EventSystem.current is null", null);
                return;
            }

            PointerEventData ped = new PointerEventData(EventSystem.current) { position = screenPos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            if (results.Count == 0)
            {
                TournamentJoinDebug.LogClickBlocked(screenPos, joinButton, "No UI raycast hits inside JOIN rect", null);
                return;
            }

            GameObject top = results[0].gameObject;
            if (IsJoinButtonHit(top)) return;

            TournamentJoinDebug.LogClickBlocked(
                screenPos,
                joinButton,
                "Another UI element is above Join_duel_1v1",
                top);
        }

        private static bool WasPointerPressedThisFrame(out Vector2 screenPos)
        {
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                screenPos = Input.GetTouch(0).position;
                return true;
            }

            screenPos = default;
            return false;
        }

        private bool IsJoinButtonHit(GameObject hit)
        {
            if (!hit || !joinButton) return false;
            return hit.transform == joinButton || hit.transform.IsChildOf(joinButton);
        }
    }
}
