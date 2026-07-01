using System;
using Mkey;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// JOIN hit area bound to one TournamentDefinition instance (no shared closure).
    /// </summary>
    public class TournamentJoinButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] private string tournamentId;
        [SerializeField] private string tournamentName;

        private TournamentDefinition tournament;
        private Action<TournamentDefinition> onJoin;

        public TournamentDefinition Tournament => tournament;

        public void Bind(TournamentDefinition definition, Action<TournamentDefinition> joinCallback)
        {
            tournament = definition;
            onJoin = joinCallback;
            tournamentId = definition != null ? definition.id : string.Empty;
            tournamentName = definition != null ? definition.displayName : string.Empty;

            Button button = GetComponent<Button>();
            if (!button)
                button = gameObject.AddComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);

            if (definition != null && definition.id == TournamentJoinDebug.FirstJoinId)
            {
                Image hit = GetComponent<Image>();
                TournamentJoinDebug.Log(
                    $"Join button bound: onClick listeners={button.onClick.GetPersistentEventCount()}+1, " +
                    $"interactable={button.interactable}, raycast={(hit ? hit.raycastTarget : false)}");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (tournament == null || tournament.id != TournamentJoinDebug.FirstJoinId)
                return;

            Debug.Log("[TournamentJoin] JOIN BUTTON CLICKED (PointerDown)");
            TournamentJoinDebug.LogPointerDown(gameObject, "IPointerDownHandler");
            TournamentJoinDebug.LogUiState(gameObject, "PointerDown");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (tournament == null || tournament.id != TournamentJoinDebug.FirstJoinId)
                return;

            TournamentJoinDebug.LogPointerClick(gameObject, "IPointerClickHandler");
        }

        private void OnButtonClicked()
        {
            if (tournament != null && tournament.id == TournamentJoinDebug.FirstJoinId)
                TournamentJoinDebug.LogButtonOnClickExecuted(name);

            if (!TournamentJoinFlowGuard.CheckCanStartJoin("TournamentJoinButton.OnButtonClicked"))
                return;

            if (tournament == null)
            {
                TournamentJoinDebug.LogError($"JOIN button '{name}' has no TournamentDefinition bound");
                return;
            }

            if (SoundMaster.Instance)
                SoundMaster.Instance.SoundPlayClick(0.12f, null);

            TournamentJoinDebug.Log($"JOIN clicked: button={name}, id={tournament.id}, name={tournament.displayName}");
            onJoin?.Invoke(tournament);
        }
    }
}
