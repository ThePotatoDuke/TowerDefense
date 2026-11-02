using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Movement = MenuCritter.Movement;

// The menu critter's Inspector shows only the settings its Movement uses (a Checker Slime's Checker Hop
// section, not the Tibbits' riding or the slimes' hop pattern). The rest keep their values, just hidden.
[CustomEditor(typeof(MenuCritter)), CanEditMultipleObjects]
public class MenuCritterEditor : Editor
{
    private static readonly string[] Hops = { "hopHeight", "hopDuration", "hopInterval", "hopDistance" };
    private static readonly string[] SquashAndStretch = { "squash", "stretch" };
    private static readonly string[] HopPattern =
    {
        "hopPattern", "longHopDistance", "longHopHeight", "longHopDuration", "longHopWait", "backChance",
        "longForwardChance", "minBackForward", "maxBackForward",
    };
    private static readonly string[] Flying = { "flyHeight", "waveAmplitude", "waveLength", "bobHeight", "bobsPerSecond" };
    private static readonly string[] Digging = { "digFrames", "digFramesPerSecond", "peekFrames", "peekFramesPerSecond", "idleTime" };
    private static readonly string[] Riding =
    {
        "rides", "rideTime", "jumpRange", "jumpHeight", "jumpSpeed", "jumpTime", "searchInterval", "rideCooldown",
        "sameWay", "wrongWayGrace",
    };
    private static readonly string[] Rolling =
    {
        "markerOuter", "markerMiddle", "dropWarningTime", "markerPopTime", "markerSpin", "dropFallTime", "landShake",
        "landShakeTime", "rollFrameDistance",
    };
    private static readonly string[] CheckerHop =
    {
        "altFrames", "checkerHopHeight", "checkerHopHeightPerSlime", "checkerHopGap", "checkerHopAirSpeed",
        "checkerSquashTime", "checkerSpringBackTime",
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty movementProperty = serializedObject.FindProperty("movement");
        // With several selected that move differently, show everything
        bool mixed = movementProperty.hasMultipleDifferentValues;
        var movement = (Movement)movementProperty.enumValueIndex;
        var hidden = new HashSet<string>(HiddenFor(movement));

        SerializedProperty property = serializedObject.GetIterator();
        for (bool enter = true; property.NextVisible(enter); enter = false)
        {
            if (property.name == "m_Script" || (!mixed && hidden.Contains(property.name))) continue;

            // A Checker Slime's squash and stretch, without the rest of the hops section and its heading
            if (!mixed && movement == Movement.CheckerHop && property.name == "squash")
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Squash And Stretch", EditorStyles.boldLabel);
            }
            EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
    }

    // The settings this movement never uses
    private static IEnumerable<string> HiddenFor(Movement movement)
    {
        var hidden = new List<string>();
        bool hops = movement == Movement.Hop || movement == Movement.Skip;
        if (!hops) hidden.AddRange(Hops);
        if (!hops && movement != Movement.CheckerHop) hidden.AddRange(SquashAndStretch);
        if (!hops) hidden.Add("airSprite");
        if (movement != Movement.Hop) hidden.AddRange(HopPattern);
        if (movement != Movement.Fly) hidden.AddRange(Flying);
        if (movement != Movement.Dig) hidden.AddRange(Digging);
        if (movement != Movement.Skip) hidden.AddRange(Riding);
        if (movement != Movement.Roll) hidden.AddRange(Rolling);
        if (movement != Movement.CheckerHop) hidden.AddRange(CheckerHop);
        if (movement == Movement.Roll) hidden.Add("facesLeft"); // it never turns round
        if (movement == Movement.CheckerHop) hidden.Add("speed"); // its beat sets its speed
        return hidden;
    }
}
