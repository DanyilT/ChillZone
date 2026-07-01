using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChillZone.Utils.Native
{
    /// <summary>
    /// Scene-local singleton that handles the Android hardware back button (and
    /// Escape key in the editor) via a stack of registered handlers.
    ///
    /// Each scene bootstraps its own root action (e.g. "go back to Game scene").
    /// Windows auto-push when shown and auto-pop when closed, so the back button
    /// always dismisses the topmost modal before navigating away.
    /// </summary>
    public class BackNavigationController : MonoBehaviour
    {
        public static BackNavigationController Instance { get; private set; }
        private readonly Stack<Action> _stack = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (_stack.Count > 0) _stack.Pop()?.Invoke();
        }

        #region public api

        /// <summary>Register a back action. The most recently pushed action fires first.</summary>
        public void Push(Action onBack) => _stack.Push(onBack);

        /// <summary>Remove the top back action without invoking it.</summary>
        public void Pop() { if (_stack.Count > 0) _stack.Pop(); }

        /// <summary>Clear all registered back actions.</summary>
        public void Clear() => _stack.Clear();

        public bool HasHandlers => _stack.Count > 0;

        #endregion
    }
}
