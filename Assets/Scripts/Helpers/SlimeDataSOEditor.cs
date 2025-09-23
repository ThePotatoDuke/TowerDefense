#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SlimeDataSO))]
public class SlimeDataSOEditor : Editor
{
    private bool showGeneralSettings = true;
    private bool showBounceSettings = true;
    private bool showHopDistances = true;
    private bool showHopPattern = true;
    private bool showLongHopTweaks = true;
    private bool showStretchSquash = true;

    public override void OnInspectorGUI()
    {
        SlimeDataSO data = (SlimeDataSO)target;

        EditorGUI.BeginChangeCheck();

        // --- Base Enemy Fields ---
        showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
        if (showGeneralSettings)
        {
            EditorGUILayout.LabelField("Base Enemy Settings", EditorStyles.boldLabel);
            data.enemyName = EditorGUILayout.TextField("Enemy Name", data.enemyName);
            data.maxHealth = EditorGUILayout.FloatField("Max Health", data.maxHealth);
            data.contactDamage = EditorGUILayout.FloatField("Contact Damage", data.contactDamage);
            EditorGUILayout.Space();
        }

        // --- Bounce Settings ---
        showBounceSettings = EditorGUILayout.Foldout(showBounceSettings, "Bounce Settings", true);
        if (showBounceSettings)
        {
            data.bounceHeight = EditorGUILayout.FloatField("Bounce Height", data.bounceHeight);
            data.bounceDuration = EditorGUILayout.FloatField("Bounce Duration", data.bounceDuration);
            data.bounceInterval = EditorGUILayout.FloatField("Bounce Interval", data.bounceInterval);
            EditorGUILayout.Space();
        }

        // --- Hop Pattern ---
        showHopPattern = EditorGUILayout.Foldout(showHopPattern, "Hop Pattern", true);
        if (showHopPattern)
        {
            data.hopPattern = (SlimeDataSO.HopPattern)EditorGUILayout.EnumPopup("Hop Pattern", data.hopPattern);

            EditorGUILayout.Space();
        }

        // --- Hop Distances & Pattern-Specific Fields ---
        showHopDistances = EditorGUILayout.Foldout(showHopDistances, "Hop Distances", true);
        if (showHopDistances)
        {
            switch (data.hopPattern)
            {
                case SlimeDataSO.HopPattern.Regular:
                    data.baseHopDistance = EditorGUILayout.FloatField("Base Hop Distance", data.baseHopDistance);
                    break;

                case SlimeDataSO.HopPattern.ShortShortLong:
                    data.baseHopDistance = EditorGUILayout.FloatField("Base Hop Distance", data.baseHopDistance);
                    data.longHopDistanceMultiplier = EditorGUILayout.FloatField("Long Hop Distance Multiplier", data.longHopDistanceMultiplier);
                    data.longHopWaitTime = EditorGUILayout.FloatField("Long Hop Wait Time", data.longHopWaitTime);
                    break;

                case SlimeDataSO.HopPattern.BackAndForward:
                    data.baseHopDistance = EditorGUILayout.FloatField("Base Hop Distance", data.baseHopDistance);
                    data.longHopDistanceMultiplier = EditorGUILayout.FloatField("Long Hop Distance Multiplier", data.longHopDistanceMultiplier);
                    data.minBackForward = EditorGUILayout.IntField("Min BackForward", data.minBackForward);
                    data.maxBackForward = EditorGUILayout.IntField("Max BackForward", data.maxBackForward);
                    data.backChance = EditorGUILayout.Slider("Back Chance", data.backChance, 0f, 1f);
                    data.longForwardChance = EditorGUILayout.Slider("Long Forward Chance", data.longForwardChance, 0f, 1f);
                    break;
            }

            EditorGUILayout.Space();
        }

        // --- Long Hop Tweaks ---
        showLongHopTweaks = EditorGUILayout.Foldout(showLongHopTweaks, "Long Hop Tweaks", true);
        if (showLongHopTweaks)
        {
            if (data.hopPattern != SlimeDataSO.HopPattern.Regular)
            {
                data.longHopHeightMultiplier = EditorGUILayout.FloatField("Long Hop Height Multiplier", data.longHopHeightMultiplier);
                data.longHopDurationMultiplier = EditorGUILayout.FloatField("Long Hop Duration Multiplier", data.longHopDurationMultiplier);
            }
            EditorGUILayout.Space();
        }

        // --- Stretch/Squash ---
        showStretchSquash = EditorGUILayout.Foldout(showStretchSquash, "Stretch/Squash", true);
        if (showStretchSquash)
        {
            data.takeoffStretch = EditorGUILayout.FloatField("Takeoff Stretch", data.takeoffStretch);
            data.fallStretch = EditorGUILayout.FloatField("Fall Stretch", data.fallStretch);
            data.squash = EditorGUILayout.FloatField("Squash", data.squash);
            EditorGUILayout.Space();
        }

        // Mark dirty if anything changed
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
