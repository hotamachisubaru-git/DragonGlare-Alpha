using UnityEngine;

namespace DragonGlareUnity
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("DragonGlare/Bootstrap")]
    public sealed class DragonGlareBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (FindAnyObjectByType<DragonGlareBehaviour>() == null)
                gameObject.AddComponent<DragonGlareBehaviour>();
        }
    }
}
