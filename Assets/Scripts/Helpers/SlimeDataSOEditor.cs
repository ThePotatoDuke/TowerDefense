using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SlimeDataSO))]
public class SlimeDataSOEditor : Editor
{
    SerializedProperty enemyNameProp;
    SerializedProperty colorProp;
    SerializedProperty maxHealthProp;
    SerializedProperty contactDamageProp;

    SerializedProperty hopPatternProp;
    SerializedProperty baseHopDistanceProp;
    SerializedProperty longHopMultiplierProp;
    SerializedProperty longHopWaitTimeProp;
    SerializedProperty minBackForwardProp;
    SerializedProperty maxBackForwardProp;
    SerializedProperty backChanceProp;
    SerializedProperty longForwardChanceProp;
    SerializedProperty leapHeightMultiplierProp;
    SerializedProperty leapDurationMultiplierProp;

    SerializedProperty bounceHeightProp;
    SerializedProperty bounceDurationProp;
    SerializedProperty bounceIntervalProp;

    SerializedProperty takeoffStretchProp;
    SerializedProperty fallStretchProp;
    SerializedProperty squashProp;

    private void OnEnable()
    {
        enemyNameProp = serializedObject.FindProperty("enemyName");
        colorProp = serializedObject.FindProperty("color");
        maxHealthProp = serializedObject.FindProperty("maxHealth");
        contactDamageProp = serializedObject.FindProperty("contactDamage");

        hopPatternProp = serializedObject.FindProperty("hopPattern");
        baseHopDistanceProp = serializedObject.FindProperty("baseHopDistance");
        longHopMultiplierProp = serializedObject.FindProperty("longHopMultiplier");
        longHopWaitTimeProp = serializedObject.FindProperty("longHopWaitTime");
        minBackForwardProp = serializedObject.FindProperty("minBackForward");
        maxBackForwardProp = serializedObject.FindProperty("maxBackForward");
        backChanceProp = serializedObject.FindProperty("backChance");
        longForwardChanceProp = serializedObject.FindProperty("longForwardChance");
        leapHeightMultiplierProp = serializedObject.FindProperty("leapHeightMultiplier");
        leapDurationMultiplierProp = serializedObject.FindProperty("leapDurationMultiplier");

        bounceHeightProp = serializedObject.FindProperty("bounceHeight");
        bounceDurationProp = serializedObject.FindProperty("bounceDuration");
        bounceIntervalProp = serializedObject.FindProperty("bounceInterval");

        takeoffStretchProp = serializedObject.FindProperty("takeoffStretch");
        fallStretchProp = serializedObject.FindProperty("fallStretch");
        squashProp = serializedObject.FindProperty("squash");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // General
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enemyNameProp);
        EditorGUILayout.PropertyField(colorProp);
        EditorGUILayout.PropertyField(maxHealthProp);
        EditorGUILayout.PropertyField(contactDamageProp);

        EditorGUILayout.Space();

        // Movement
        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(hopPatternProp);
        EditorGUILayout.PropertyField(baseHopDistanceProp);

        SlimeDataSO.HopPattern pattern = (SlimeDataSO.HopPattern)hopPatternProp.enumValueIndex;
        switch (pattern)
        {
            case SlimeDataSO.HopPattern.Regular:
                // Only base hop, no extras
                break;

            case SlimeDataSO.HopPattern.ShortShortLong:
                EditorGUILayout.PropertyField(longHopMultiplierProp);
                EditorGUILayout.PropertyField(longHopWaitTimeProp);
                break;

            case SlimeDataSO.HopPattern.BackAndForward:
                EditorGUILayout.PropertyField(minBackForwardProp);
                EditorGUILayout.PropertyField(maxBackForwardProp);
                EditorGUILayout.PropertyField(backChanceProp);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Leap Forward Tweaks", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(longHopMultiplierProp);
                EditorGUILayout.PropertyField(leapHeightMultiplierProp);
                EditorGUILayout.PropertyField(leapDurationMultiplierProp);

                EditorGUILayout.PropertyField(longForwardChanceProp,
                    new GUIContent("Long Forward Chance", "Chance that a forward hop after backward hop will be long."));
                break;
        }

        EditorGUILayout.Space();

        // Bounce
        EditorGUILayout.LabelField("Bounce", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bounceHeightProp);
        EditorGUILayout.PropertyField(bounceDurationProp);
        EditorGUILayout.PropertyField(bounceIntervalProp);

        EditorGUILayout.Space();

        // Visual
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(takeoffStretchProp);
        EditorGUILayout.PropertyField(fallStretchProp);
        EditorGUILayout.PropertyField(squashProp);

        serializedObject.ApplyModifiedProperties();
    }
}
