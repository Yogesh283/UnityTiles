using UnityEngine;
using UnityEngine.EventSystems;

namespace Mkey
{
    /// <summary>
    /// Runs before default Awake so duplicate scene EventSystems are removed before OnEnable warns.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class UiEventSystemSelfGuard : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<EventSystem>())
                UiEventSystemGuard.EnforceSingle();
        }
    }
}
