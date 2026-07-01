using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif

namespace ChillZone.Utils.Native
{
    /// <summary>
    /// Shows an OS-native confirm dialog. Android → android.app.AlertDialog via JNI; editor →
    /// EditorUtility.DisplayDialog; other platforms → invoke onConfirm directly. Android button
    /// callbacks fire on the Android UI thread, so they are marshalled back to Unity's main
    /// thread (via <see cref="NativeDialogRunner"/>) before the C# callbacks run.
    /// </summary>
    public static class NativeConfirmDialog
    {
        public static void Show(string title, string message, string confirmLabel, string cancelLabel, Action onConfirm, Action onCancel = null)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.DisplayDialog(title, message, confirmLabel, cancelLabel))
                onConfirm?.Invoke();
            else
                onCancel?.Invoke();
#elif UNITY_ANDROID
            ShowAndroid(title, message, confirmLabel, cancelLabel, onConfirm, onCancel);
#else
            onConfirm?.Invoke();
#endif
        }

        /// <summary>Single-button informational dialog (e.g. announcing a code unlock). No cancel button.</summary>
        public static void ShowInfo(string title, string message, string dismissLabel = "OK", Action onDismiss = null)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(title, message, dismissLabel);
            onDismiss?.Invoke();
#elif UNITY_ANDROID
            ShowAndroidInfo(title, message, dismissLabel, onDismiss);
#else
            onDismiss?.Invoke();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ShowAndroid(string title, string message, string confirm, string cancel, Action onConfirm, Action onCancel)
        {
            var runner = NativeDialogRunner.Create(onConfirm, onCancel);

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = player.GetStatic<AndroidJavaObject>("currentActivity");

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                var builder = new AndroidJavaObject("android.app.AlertDialog$Builder", activity);
                builder.Call<AndroidJavaObject>("setTitle", title);
                builder.Call<AndroidJavaObject>("setMessage", message);
                builder.Call<AndroidJavaObject>("setCancelable", true);
                builder.Call<AndroidJavaObject>("setPositiveButton", confirm, new DialogClickListener(() => runner.Resolve(true)));
                builder.Call<AndroidJavaObject>("setNegativeButton", cancel,  new DialogClickListener(() => runner.Resolve(false)));
                builder.Call<AndroidJavaObject>("setOnCancelListener", new DialogCancelListener(() => runner.Resolve(false)));
                builder.Call<AndroidJavaObject>("create").Call("show");
            }));
        }

        private static void ShowAndroidInfo(string title, string message, string dismiss, Action onDismiss)
        {
            var runner = NativeDialogRunner.Create(onDismiss, onDismiss);

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = player.GetStatic<AndroidJavaObject>("currentActivity");

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                var builder = new AndroidJavaObject("android.app.AlertDialog$Builder", activity);
                builder.Call<AndroidJavaObject>("setTitle", title);
                builder.Call<AndroidJavaObject>("setMessage", message);
                builder.Call<AndroidJavaObject>("setCancelable", true);
                builder.Call<AndroidJavaObject>("setPositiveButton", dismiss, new DialogClickListener(() => runner.Resolve(true)));
                builder.Call<AndroidJavaObject>("setOnCancelListener", new DialogCancelListener(() => runner.Resolve(false)));
                builder.Call<AndroidJavaObject>("create").Call("show");
            }));
        }

        private class DialogClickListener : AndroidJavaProxy
        {
            private readonly Action _onClick;
            public DialogClickListener(Action onClick) : base("android.content.DialogInterface$OnClickListener") => _onClick = onClick;
            public void onClick(AndroidJavaObject dialog, int which) => _onClick?.Invoke();
        }

        private class DialogCancelListener : AndroidJavaProxy
        {
            private readonly Action _onCancel;
            public DialogCancelListener(Action onCancel) : base("android.content.DialogInterface$OnCancelListener") => _onCancel = onCancel;
            public void onCancel(AndroidJavaObject dialog) => _onCancel?.Invoke();
        }
#endif
    }
}
