using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    public class TournamentFloatingParticles : MonoBehaviour
    {
        private struct P { public RectTransform Rt; public float Speed; public float Phase; public Vector2 Base; }
        private P[] _p;

        private void Start()
        {
            _p = new P[16];
            for (int i = 0; i < _p.Length; i++)
            {
                RectTransform rt = TournamentUIFactory.CreateRect(transform, "P" + i);
                float s = Random.Range(6f, 18f);
                rt.sizeDelta = new Vector2(s, s);
                Vector2 pos = new Vector2(Random.Range(0.05f, 0.95f), Random.Range(0.1f, 0.9f));
                rt.anchorMin = rt.anchorMax = pos;
                Image img = rt.gameObject.AddComponent<Image>();
                img.sprite = TournamentSpriteFactory.SoftCircle;
                img.color = new Color(0.95f, 0.82f, 0.4f, Random.Range(0.06f, 0.14f));
                img.raycastTarget = false;
                _p[i] = new P { Rt = rt, Speed = Random.Range(0.15f, 0.4f), Phase = Random.Range(0f, Mathf.PI * 2f), Base = pos };
            }
        }

        private void Update()
        {
            if (_p == null) return;
            float t = Time.time;
            for (int i = 0; i < _p.Length; i++)
            {
                float bob = Mathf.Sin(t * _p[i].Speed + _p[i].Phase);
                _p[i].Rt.anchorMin = _p[i].Rt.anchorMax = _p[i].Base + new Vector2(bob * 0.006f, bob * 0.01f);
            }
        }
    }

    public class TournamentFogDrift : MonoBehaviour
    {
        private Vector2 _start;
        private void Awake() { _start = (transform as RectTransform).anchoredPosition; }
        private void Update() { (transform as RectTransform).anchoredPosition = _start + new Vector2(0f, Mathf.Sin(Time.time * 0.2f) * 12f); }
    }

    public class TournamentGoldShine : MonoBehaviour
    {
        private Image _shine;
        private float _t;
        private void Start()
        {
            _shine = TournamentUIFactory.CreateImage(transform, "Shine", new Color(1f, 0.95f, 0.7f, 0.04f));
            RectTransform rt = _shine.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 400f);
            rt.localRotation = Quaternion.Euler(0f, 0f, 15f);
            _shine.raycastTarget = false;
        }
        private void Update()
        {
            if (!_shine) return;
            _t += Time.deltaTime * 0.1f;
            float x = Mathf.PingPong(_t, 1f);
            _shine.rectTransform.anchorMin = _shine.rectTransform.anchorMax = new Vector2(x, 0.5f);
        }
    }

    public class TournamentCardGlow : MonoBehaviour
    {
        private Image _glow;
        private float _base;
        private void Start()
        {
            _glow = TournamentUIFactory.CreateImage(transform, "OuterGlow", new Color(0.95f, 0.8f, 0.35f, 0.1f), TournamentSpriteFactory.SoftCircle);
            TournamentUIFactory.StretchRect(_glow.rectTransform);
            _glow.transform.SetAsFirstSibling();
            _glow.rectTransform.offsetMin = new Vector2(-6f, -6f);
            _glow.rectTransform.offsetMax = new Vector2(6f, 6f);
            if (_glow) _base = _glow.color.a;
        }
        private void Update()
        {
            if (!_glow) return;
            Color c = _glow.color;
            c.a = _base + Mathf.Sin(Time.time * 1.6f) * 0.04f;
            _glow.color = c;
        }
    }

    public class TournamentButtonGlow : MonoBehaviour
    {
        private Image _glow;
        private void Start()
        {
            _glow = TournamentUIFactory.CreateImage(transform, "Glow", new Color(1f, 0.55f, 0.12f, 0.2f), TournamentSpriteFactory.SoftCircle);
            TournamentUIFactory.StretchRect(_glow.rectTransform);
            _glow.transform.SetAsFirstSibling();
            _glow.rectTransform.offsetMin = new Vector2(-8f, -8f);
            _glow.rectTransform.offsetMax = new Vector2(8f, 8f);
        }
        private void Update()
        {
            if (!_glow) return;
            Color c = _glow.color;
            c.a = 0.14f + Mathf.Sin(Time.time * 2.2f) * 0.08f;
            _glow.color = c;
        }
    }

    public class TournamentJoinButtonShine : MonoBehaviour
    {
        private Image _shine;
        private void Start()
        {
            _shine = TournamentUIFactory.CreateImage(transform, "Shine", new Color(1f, 1f, 1f, 0.2f));
            _shine.rectTransform.anchorMin = new Vector2(0.1f, 0.6f);
            _shine.rectTransform.anchorMax = new Vector2(0.9f, 0.95f);
            _shine.rectTransform.offsetMin = _shine.rectTransform.offsetMax = Vector2.zero;
            _shine.raycastTarget = false;
        }
        private void Update()
        {
            if (!_shine) return;
            Color c = _shine.color;
            c.a = 0.14f + Mathf.Sin(Time.time * 2f) * 0.08f;
            _shine.color = c;
        }
    }

    public class TournamentCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Vector3 _base = Vector3.one;
        private void Awake() { _base = transform.localScale; }
        public void OnPointerEnter(PointerEventData e) { transform.localScale = _base * 1.02f; }
        public void OnPointerExit(PointerEventData e) { transform.localScale = _base; }
    }

    public class TournamentCardSlideIn : MonoBehaviour
    {
        private float _delay;
        public void Configure(float d) { _delay = d; StartCoroutine(Play()); }
        private IEnumerator Play()
        {
            CanvasGroup g = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            RectTransform rt = transform as RectTransform;
            g.alpha = 0f;
            rt.localScale = Vector3.one * 0.9f;
            if (_delay > 0f) yield return new WaitForSeconds(_delay);
            float t = 0f;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                float e = 1f - Mathf.Pow(1f - t / 0.38f, 3f);
                g.alpha = e;
                rt.localScale = Vector3.LerpUnclamped(Vector3.one * 0.9f, Vector3.one, e);
                yield return null;
            }
            g.alpha = 1f;
            rt.localScale = Vector3.one;
        }
    }

    public class TournamentPageIntro : MonoBehaviour
    {
        public static void Play(GameObject root) => root.AddComponent<TournamentPageIntro>();
        private IEnumerator Start()
        {
            CanvasGroup g = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime) { g.alpha = t / 0.45f; yield return null; }
            g.alpha = 1f;
        }
    }
}
