using System.Collections;
using UnityEngine.Events;
using UnityEngine;

/*
  24.01.2022
 */

namespace Mkey
{
    public class FbEvents : MonoBehaviour
    {
        public UnityEvent <string> LoadFullNameEvent;
        public UnityEvent<string> LoadFirstNameEvent;
        public UnityEvent<string> LoadLastNameEvent;
        public UnityEvent GoodLoginEvent;
        public UnityEvent <string> FailedLoginEvent; // <result>
        public UnityEvent LogoutEvent;

        #region regular
        void Start()
        {
            FBholder.LogoutEvent += LogoutEventHandler;
            FBholder.LoginEvent += LoginEventHandler;
            FBholder.LoadTextEvent += LoadTextEventHandler; 
        }

        void OnDestroy()
        {
            FBholder.LogoutEvent -= LogoutEventHandler;
            FBholder.LoginEvent -= LoginEventHandler;
            FBholder.LoadTextEvent -= LoadTextEventHandler;
        }
        #endregion regular

        private void LoginEventHandler(bool logined, string result)
        {
            if (logined)
            {
                GoodLoginEvent?.Invoke();
            }
            else
            {
                FailedLoginEvent?.Invoke(result);
            }
        }

        private void LogoutEventHandler()
        {
            LogoutEvent?.Invoke();
        }

        private void LoadTextEventHandler(bool logined, string firstName, string lastName)
        {
            if (logined)
            {
                LoadFirstNameEvent?.Invoke(firstName);
                LoadLastNameEvent?.Invoke(lastName);
                LoadFullNameEvent?.Invoke(firstName +" "+ lastName);
            }
        }
    }
}