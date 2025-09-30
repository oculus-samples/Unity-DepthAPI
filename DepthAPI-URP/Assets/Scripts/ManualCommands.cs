using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
// New Input System
using UnityEngine.InputSystem;

/// <summary>
/// ManualCommands lets you bind a single keyboard KeyCode and/or a single
/// Oculus OVRInput RawButton to a UnityEvent per binding. Configure in Inspector.
/// </summary>
public class ManualCommands : MonoBehaviour
{
    [Serializable]
    public class Binding
    {
        [Tooltip("Optional label to help identify this binding.")]
        public string name;

        [Header("Keyboard (Input System Key)")]
        [Tooltip("Trigger when this Key is pressed (wasPressedThisFrame).")]
        public Key key = Key.None;

        [Header("OVR Input (Oculus Controllers)")]
        [Tooltip("Single OVR RawButton that triggers this binding (e.g., A, B).")]
        public OVRInput.RawButton ovrRawButton = OVRInput.RawButton.None;

        [Header("Event")]
        [Tooltip("Event invoked when this binding is triggered.")]
        public UnityEvent onTriggered;
    }

    [Tooltip("List of controls mapped to events.")]
    public List<Binding> bindings = new List<Binding>();

    private void Update()
    {
        if (bindings == null) return;

        for (int i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            if (b == null) continue;

            // Keyboard key (Input System)
            if (b.key != Key.None && WasKeyPressedThisFrame(b.key))
            {
                Trigger(b);
                continue; // avoid double-fire in same frame if also mapped
            }

            // Oculus OVRInput RawButton
            if (b.ovrRawButton != OVRInput.RawButton.None)
            {
                if (OVRInput.GetDown(b.ovrRawButton))
                {
                    Trigger(b);
                    continue;
                }
            }
        }
    }

    private static bool WasKeyPressedThisFrame(Key key)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return false;
        // Using indexer to access a specific KeyControl
        var control = kb[key];
        return control != null && control.wasPressedThisFrame;
#else
        return false;
#endif
    }

    /// <summary>
    /// Invoke a binding by its configured name.
    /// </summary>
    public void TriggerByName(string bindingName)
    {
        if (string.IsNullOrEmpty(bindingName) || bindings == null) return;
        for (int i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            if (b != null && string.Equals(b.name, bindingName, StringComparison.Ordinal))
            {
                Trigger(b);
                return;
            }
        }
    }

    /// <summary>
    /// Invoke a binding by index.
    /// </summary>
    public void TriggerIndex(int index)
    {
        if (index < 0 || bindings == null || index >= bindings.Count) return;
        var b = bindings[index];
        if (b != null) Trigger(b);
    }

    private void Trigger(Binding b)
    {
        b.onTriggered?.Invoke();
    }

}
