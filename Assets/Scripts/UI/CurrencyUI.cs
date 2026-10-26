using DG.Tweening;
using TMPro;
using UnityEngine;

// Keeps the grass counter's number up to date and gives it a little punch when grass is collected.
// The counter itself (board, icon, text) is laid out in the scene; Tools > Create UI Layout Objects
// builds a starting one.
public class CurrencyUI : MonoBehaviour
{
    [Tooltip("The whole counter; this is what punches when grass is collected.")]
    [SerializeField] private RectTransform counter;
    [SerializeField] private TMP_Text countText;

    private void OnEnable()
    {
        Currency.OnGrassChanged += HandleGrassChanged;
        if (countText != null)
            countText.text = Currency.Grass.ToString();
    }

    private void OnDisable()
    {
        Currency.OnGrassChanged -= HandleGrassChanged;
    }

    private void HandleGrassChanged(int amount)
    {
        if (countText != null)
            countText.text = amount.ToString();
        if (amount == 0 || counter == null) return; // reset on a new run, nothing to celebrate

        counter.DOKill(true);
        counter.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 0.5f).SetUpdate(true).SetLink(counter.gameObject);
    }
}
