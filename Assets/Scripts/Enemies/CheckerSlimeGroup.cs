using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

// Spawns a group of Checker Slimes in a black and white line on the path and moves it by leapfrog: the
// slime at the back jumps over all the others to land a space ahead of the front one, then the next at the
// back takes off on the beat, before that one lands, and so on, like a wheel rolling: the jumpers fly
// faster than the line, as the top of a wheel does, and with a big group several are up at once. With an
// even group the checker always holds. Ones that die leave gaps that close as the line turns over; the last
// one left hops along on its own. Each lands at the castle in turn and they pile up there like other
// enemies, taking turns at little hops on the spot.
public class CheckerSlimeGroup : MonoBehaviour
{
    private readonly List<CheckerSlimeEnemy> members = new();
    private CheckerSlimeDataSO data;
    private float splineLength = 1f;
    private int nextAtCastle; // whose turn it is to hop at the castle

    // A new group of the prefab at the start of the path, the front one furthest in. setUp is called on each
    // one as it's made (the wave manager's health, immunity and bookkeeping).
    public static void Spawn(CheckerSlimeEnemy prefab, SplineContainer path, System.Action<CheckerSlimeEnemy> setUp)
    {
        CheckerSlimeDataSO data = prefab.CheckerData;
        if (data == null || path == null) return;

        var group = new GameObject("CheckerSlimeGroup").AddComponent<CheckerSlimeGroup>();
        group.data = data;
        group.splineLength = Mathf.Max(0.01f, path.CalculateLength());

        int count = data.RollGroupSize();
        bool white = Random.value < 0.5f; // which colour leads
        for (int i = 0; i < count; i++)
        {
            CheckerSlimeEnemy slime = Instantiate(prefab);
            slime.AssignSpline(path);
            slime.PlaceInLine((count - 1 - i) * data.spacing / group.splineLength);
            slime.SetWhite(white == (i % 2 == 0));
            setUp?.Invoke(slime);
            group.members.Add(slime);
        }
        group.StartCoroutine(group.Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(data.startDelay);

        while (true)
        {
            members.RemoveAll(slime => slime == null || slime.IsDead);
            if (members.Count == 0) break;

            // On the beat: the back one leapfrogs over everyone else, to a space ahead of the front one (just
            // a hop when it's alone, and no further than the castle), and one already at the castle hops
            CheckerSlimeEnemy back = Back();
            if (back != null && !back.InAir && !back.IsDodging)
            {
                int over = members.Count - 1;
                float height = data.checkerHopHeight + data.checkerHopHeightPerSlime * over;
                back.JumpTo(Front() + data.spacing / splineLength, height, data.HopDuration(over));
            }
            HopAtCastle();

            yield return new WaitForSeconds(data.checkerHopGap);
        }
        Destroy(gameObject);
    }

    // The one furthest back that's still on its way (not at the castle)
    private CheckerSlimeEnemy Back()
    {
        CheckerSlimeEnemy back = null;
        foreach (CheckerSlimeEnemy slime in members)
            if (!slime.HasReachedEnd && (back == null || slime.LandingProgress < back.LandingProgress))
                back = slime;
        return back;
    }

    // The next one standing at the castle, in turn, hops on the spot
    private void HopAtCastle()
    {
        for (int tries = 0; tries < members.Count; tries++)
        {
            CheckerSlimeEnemy slime = members[nextAtCastle++ % members.Count];
            if (!slime.HasReachedEnd || slime.InAir) continue;
            slime.JumpTo(slime.LandingProgress, data.checkerHopHeight, data.HopDuration(0));
            return;
        }
    }

    // How far along the front one is, or will be once it lands
    private float Front()
    {
        float front = 0f;
        foreach (CheckerSlimeEnemy slime in members)
            front = Mathf.Max(front, slime.LandingProgress);
        return front;
    }
}
