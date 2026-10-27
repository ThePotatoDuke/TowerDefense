using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// The weapons the player carries. Picking one up puts it in a free slot and switches to it (or swaps out
// the one in hand when every slot is full); the others wait in their slots, switched off, until switched
// back to (Q or the gamepad's shoulder buttons cycle, 1-3 pick a slot). WeaponBarUI shows the slots.
public class WeaponInventory : MonoBehaviour
{
    public class Slot
    {
        public WeaponPickupSO weapon;
        public WeaponBase instance;
    }

    [SerializeField] private int maxSlots = 3;

    private readonly List<Slot> slots = new();
    private GameObject owner;
    private Transform hand;

    public IReadOnlyList<Slot> Slots => slots;
    public int MaxSlots => maxSlots;
    // The slot in hand, or -1 with no weapons
    public int CurrentIndex { get; private set; } = -1;
    public WeaponBase Current => CurrentIndex >= 0 ? slots[CurrentIndex].instance : null;

    // Weapons added, swapped or switched between
    public event Action OnChanged;

    public void Setup(GameObject owner, Transform hand)
    {
        this.owner = owner;
        this.hand = hand;
    }

    public bool Has(WeaponPickupSO weapon) => slots.Exists(slot => slot.weapon == weapon);

    // Adds the weapon and switches to it; one already carried is just switched to
    public void Add(WeaponPickupSO weapon)
    {
        if (weapon == null || weapon.weaponPrefab == null) return;

        int carried = slots.FindIndex(slot => slot.weapon == weapon);
        if (carried >= 0)
        {
            Select(carried);
            return;
        }

        GameObject weaponObject = Instantiate(weapon.weaponPrefab, hand.position, hand.rotation, hand);
        var newSlot = new Slot { weapon = weapon, instance = weaponObject.GetComponent<WeaponBase>() };
        newSlot.instance.Initialize(owner, hand);
        weaponObject.SetActive(false);

        if (slots.Count < maxSlots)
        {
            slots.Add(newSlot);
            Select(slots.Count - 1);
        }
        else
        {
            // Full: the new one takes the place of the one in hand
            Destroy(slots[CurrentIndex].instance.gameObject);
            slots[CurrentIndex] = newSlot;
            int index = CurrentIndex;
            CurrentIndex = -1;
            Select(index);
        }
    }

    public void Select(int index)
    {
        if (index < 0 || index >= slots.Count || index == CurrentIndex) return;

        if (Current != null)
            Current.gameObject.SetActive(false);
        CurrentIndex = index;

        GameObject drawn = Current.gameObject;
        drawn.SetActive(true);
        drawn.transform.DOKill(true);
        drawn.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 6, 0.5f).SetLink(drawn);

        OnChanged?.Invoke();
    }

    // Steps through the slots (1 = next, -1 = previous), wrapping round
    public void Cycle(int step)
    {
        if (slots.Count < 2) return;
        Select(((CurrentIndex + step) % slots.Count + slots.Count) % slots.Count);
    }
}
