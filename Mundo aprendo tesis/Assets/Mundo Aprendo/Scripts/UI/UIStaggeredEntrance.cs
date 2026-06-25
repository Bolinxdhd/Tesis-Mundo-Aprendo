using System.Collections;
using UnityEngine;

namespace Bolin
{
    public class UIStaggeredEntrance : MonoBehaviour
    {
        [SerializeField] private RectTransform[] items = new RectTransform[0];
        [SerializeField] private CanvasGroup[] itemGroups = new CanvasGroup[0];
        [SerializeField, Min(0f)] private float delayBetweenItems = 0.08f;
        [SerializeField, Min(0.05f)] private float itemDuration = 0.22f;
        [SerializeField, Range(0.8f, 1f)] private float startScale = 0.9f;

        private Coroutine entranceRoutine;

        private void OnEnable()
        {
            if (entranceRoutine != null) StopCoroutine(entranceRoutine);
            entranceRoutine = StartCoroutine(EntranceRoutine());
        }

        private IEnumerator EntranceRoutine()
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null) items[i].localScale = Vector3.one * startScale;
                if (i < itemGroups.Length && itemGroups[i] != null) itemGroups[i].alpha = 0f;
            }

            for (int i = 0; i < items.Length; i++)
            {
                RectTransform item = items[i];
                CanvasGroup group = i < itemGroups.Length ? itemGroups[i] : null;
                float elapsed = 0f;
                while (elapsed < itemDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / itemDuration));
                    if (item != null) item.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, t);
                    if (group != null) group.alpha = t;
                    yield return null;
                }

                if (item != null) item.localScale = Vector3.one;
                if (group != null) group.alpha = 1f;
                if (delayBetweenItems > 0f) yield return new WaitForSecondsRealtime(delayBetweenItems);
            }

            entranceRoutine = null;
        }

        private void OnDisable()
        {
            if (entranceRoutine != null) StopCoroutine(entranceRoutine);
            entranceRoutine = null;
        }
    }
}
