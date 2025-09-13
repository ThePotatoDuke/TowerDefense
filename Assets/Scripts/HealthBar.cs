using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private GameObject hasHealthGameobject;

    private IHasHealth hasHealth;
    [SerializeField] private Image barImage; // drag Bar child here
    void Start()
    {
        hasHealth = hasHealthGameobject.GetComponent<IHasHealth>();

        if (hasHealth == null)
        {
            Debug.LogError("Game Object " + hasHealthGameobject + "does not have a component that implements IHasHealth");
        }

        hasHealth.OnHealthChanged += HasProgress_OnHealthChanged;
        Hide();
    }

    private void HasProgress_OnHealthChanged(float healthRatio)
    {
        barImage.fillAmount = healthRatio;
        if (healthRatio == 0 || healthRatio == 1)
        {
            Hide();
        }
        else
        {
            Show();
        }
        barImage.fillAmount = healthRatio;
    }
    private void Show()
    {
        gameObject.SetActive(true);

    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
