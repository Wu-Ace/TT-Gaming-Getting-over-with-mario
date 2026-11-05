using System;
using System.Collections;
using UnityEngine;

namespace Extension
{
    public static class MonoBehaviourExtensions
    {
        public static Coroutine Invoke(this MonoBehaviour monoBehaviour, float seconds, Action action) =>
            monoBehaviour.StartCoroutine(DelayAction(seconds, action));

        public static void CancelInvoke(this MonoBehaviour monoBehaviour, Coroutine coroutine) =>
            monoBehaviour.StopCoroutine(coroutine);

        private static IEnumerator DelayAction(float delayTime, Action action)
        {
            yield return new WaitForSeconds(delayTime);
            action();
        }
    }
}