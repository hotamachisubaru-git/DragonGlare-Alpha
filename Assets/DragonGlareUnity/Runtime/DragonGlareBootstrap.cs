#nullable enable
using Lumis;
using Lumis.Unity;
using UnityEngine;

namespace DragonGlareUnity
{
    /// <summary>
    /// The RPG initializes only after Lumis Security approves startup.
    /// Keep this component on the same root GameObject as LumisAntiCheat.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LumisAntiCheat))]
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("DragonGlare/Bootstrap")]
    public sealed class DragonGlareBootstrap : MonoBehaviour
    {
        private LumisAntiCheat? antiCheat;
        private DragonGlareBehaviour? behaviour;

        private void Awake()
        {
            antiCheat = GetComponent<LumisAntiCheat>();
            if (antiCheat == null || !antiCheat.IsReady)
            {
                Debug.LogError("DragonGlare: Lumis Security did not approve startup. The RPG will not be created.", this);
                enabled = false;
                return;
            }

            // The event is subscribed before gameplay creation: if a runtime
            // check fails later, progress saving is blocked before quitting.
            antiCheat.ViolationDetected += OnLumisViolation;

            behaviour = FindAnyObjectByType<DragonGlareBehaviour>();
            if (behaviour == null)
                behaviour = gameObject.AddComponent<DragonGlareBehaviour>();
        }

        private void OnLumisViolation(AntiCheatViolationEventArgs violation)
        {
            if (behaviour != null)
                behaviour.BlockSavesForSecurityViolation(violation.Message);
        }

        private void OnDestroy()
        {
            if (antiCheat != null)
                antiCheat.ViolationDetected -= OnLumisViolation;
        }
    }
}
