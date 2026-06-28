using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/*
    22.11.2023 - fly target
    26.01.2022 - random distance
    25.11.2020 - check parent canvas
    08.10.2020 - first


 */
namespace Mkey
{
    public class GUIFlyer : MonoBehaviour
    {

        public Text text;
        [SerializeField]
        private float distanceMax = 300;
        [SerializeField]
        private float distanceMin = 100;
        [SerializeField]
        private float flyTime = 1.5f;
        [SerializeField]
        private EaseAnim ease = EaseAnim.EaseOutCubic;
        [SerializeField]
        private float delay = 0;
        [SerializeField]
        private bool destroy = true;
        [SerializeField]
        private bool autoFly = true;
        [SerializeField]
        private UnityEvent StartEvent;
        [SerializeField]
        private UnityEvent EndEvent;
        [SerializeField]
        private float flyToTragtetSpeed = 300f;

        private Vector3 flyTargetPosW;
        private float flyTimeToTarget = 1f;
        private bool flyToTarget = false;


        private void Start()
        {
           if(autoFly) Fly();
        }

        public void Fly(string flyerText)
        {
            if (text) text.text = flyerText;
            Fly();
        }

        public void Fly()
        {
            //Debug.Log("fly to target: " + flyToTarget);
            StartEvent?.Invoke();
            Vector3 pos = transform.localPosition;
            float distance = UnityEngine.Random.Range(distanceMin, distanceMax);
            SimpleTween.Value(gameObject, 0f, distance, flyTime).SetOnUpdate((float val) =>
            {
                if (this) transform.localPosition = pos + new Vector3(0, val);
            })
            .SetEase(ease)
            .SetDelay(delay)
            .AddCompleteCallBack(() =>
            {
                if (!flyToTarget)
                {
                    EndEvent?.Invoke();
                    if (this && destroy) Destroy(gameObject);
                }
                else
                {
                    flyTimeToTarget = Vector3.Distance(flyTargetPosW, transform.position) / flyToTragtetSpeed;
                    SimpleTween.Move(gameObject, transform.position, flyTargetPosW, flyTimeToTarget).AddCompleteCallBack(() =>
                    {
                        EndEvent?.Invoke();
                        if (this && destroy) Destroy(gameObject);
                    });
                }
            });
        }

        public GUIFlyer CreateFlyer(Canvas parentCanvas, string flyerText)
        {
            if (!parentCanvas) return null;
            GUIFlyer gF = Instantiate(this, parentCanvas.transform);
            if (gF && gF.text) gF.text.text = flyerText;
            return gF;
        }

        public GUIFlyer CreateFlyer(Canvas parentCanvas, string flyerText, Vector3 flyTargetPosW)
        {
            GUIFlyer gF = CreateFlyer(parentCanvas, flyerText);
            gF.flyToTarget = true;
            gF.flyTargetPosW = flyTargetPosW;
            return gF;
        }
    }
}
