using System.Collections;
using System.Collections.Generic;
using Mkey;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Loads remote avatar URLs into UI Images; falls back to local AvatarsHolder sprite.
    /// </summary>
    public class TournamentAvatarLoader : MonoBehaviour
    {
        private static TournamentAvatarLoader instance;
        private readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static TournamentAvatarLoader Instance
        {
            get
            {
                if (instance) return instance;
                GameObject host = new GameObject(nameof(TournamentAvatarLoader));
                instance = host.AddComponent<TournamentAvatarLoader>();
                DontDestroyOnLoad(host);
                return instance;
            }
        }

        public void Apply(Image target, string avatarUrl, bool isLocalPlayer)
        {
            if (!target) return;

            Sprite fallback = GetFallbackSprite(isLocalPlayer);
            target.sprite = fallback;
            target.color = Color.white;

            if (string.IsNullOrEmpty(avatarUrl))
                return;

            if (cache.TryGetValue(avatarUrl, out Sprite cached) && cached)
            {
                target.sprite = cached;
                return;
            }

            StartCoroutine(LoadRoutine(avatarUrl, target, fallback));
        }

        private static Sprite GetFallbackSprite(bool isLocalPlayer)
        {
            if (isLocalPlayer && AvatarsHolder.Instance)
                return AvatarsHolder.Instance.GetAvatarSprite();

            if (AvatarsHolder.Instance && AvatarsHolder.Instance.avatars != null &&
                AvatarsHolder.Instance.avatars.Length > 0)
            {
                int index = Mathf.Abs(isLocalPlayer ? AvatarsHolder.AvatarIndex : 1) %
                            AvatarsHolder.Instance.avatars.Length;
                return AvatarsHolder.Instance.avatars[index];
            }

            return null;
        }

        private IEnumerator LoadRoutine(string url, Image target, Sprite fallback)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (!target)
                yield break;

            if (request.result != UnityWebRequest.Result.Success)
            {
                target.sprite = fallback;
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (!texture)
            {
                target.sprite = fallback;
                yield break;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            cache[url] = sprite;
            target.sprite = sprite;
        }
    }
}
