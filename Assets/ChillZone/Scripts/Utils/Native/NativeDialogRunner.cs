#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace ChillZone.Utils.Native
{
    /// <summary>
    /// Marshals a native Android dialog result back to Unity's main thread. The AlertDialog
    /// button listeners run on the Android UI thread and only call <see cref="Resolve"/>
    /// (thread-safe flag); the queued callback then runs in Update on the main thread.
    /// </summary>
    internal class NativeDialogRunner : MonoBehaviour
    {
        private Action _onConfirm;
        private Action _onCancel;
        private volatile int _result; // 0 = pending, 1 = confirm, 2 = cancel

        public static NativeDialogRunner Create(Action onConfirm, Action onCancel)
        {
            var go = new GameObject(nameof(NativeDialogRunner));
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<NativeDialogRunner>();
            runner._onConfirm = onConfirm;
            runner._onCancel  = onCancel;
            return runner;
        }

        public void Resolve(bool confirmed) => _result = confirmed ? 1 : 2;

        private void Update()
        {
            if (_result == 0) return;

            var confirmed = _result == 1;
            _result = 0;
            if (confirmed) _onConfirm?.Invoke();
            else           _onCancel?.Invoke();

            Destroy(gameObject);
        }
    }
}
#endif
