using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DestructionRoyale.UI
{
    public class DamageIndicator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject indicatorPrefab;
        [SerializeField] private Transform indicatorContainer;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeSpeed = 2f;

        [Header("Colors")]
        [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private Color healColor = new Color(0f, 1f, 0f, 0.8f);

        private List<IndicatorEntry> activeIndicators = new List<IndicatorEntry>();
        private Transform playerTransform;

        private struct IndicatorEntry
        {
            public RectTransform rectTransform;
            public Image image;
            public Vector3 damageDirection;
            public float timer;
        }

        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        public void ShowDamageFromDirection(Vector3 damageSource)
        {
            if (indicatorPrefab == null || indicatorContainer == null || playerTransform == null) return;

            GameObject indicator = Instantiate(indicatorPrefab, indicatorContainer);
            RectTransform rect = indicator.GetComponent<RectTransform>();
            Image img = indicator.GetComponent<Image>();

            if (img != null) img.color = damageColor;

            Vector3 direction = (damageSource - playerTransform.position).normalized;

            activeIndicators.Add(new IndicatorEntry
            {
                rectTransform = rect,
                image = img,
                damageDirection = direction,
                timer = displayDuration
            });
        }

        private void Update()
        {
            for (int i = activeIndicators.Count - 1; i >= 0; i--)
            {
                IndicatorEntry entry = activeIndicators[i];
                entry.timer -= Time.deltaTime;

                if (entry.timer <= 0f)
                {
                    if (entry.rectTransform != null)
                        Destroy(entry.rectTransform.gameObject);
                    activeIndicators.RemoveAt(i);
                    continue;
                }

                if (playerTransform != null && entry.rectTransform != null)
                {
                    Vector3 forward = playerTransform.forward;
                    forward.y = 0;
                    float angle = Vector3.SignedAngle(forward, entry.damageDirection, Vector3.up);
                    entry.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

                    if (entry.image != null)
                    {
                        float alpha = Mathf.Lerp(0f, damageColor.a, entry.timer / displayDuration);
                        Color c = entry.image.color;
                        c.a = alpha;
                        entry.image.color = c;
                    }
                }

                activeIndicators[i] = entry;
            }
        }
    }
}
