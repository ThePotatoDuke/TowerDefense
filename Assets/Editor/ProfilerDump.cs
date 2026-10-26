using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

// Tools > Dump Profiler Spikes: reads the frames currently held in the Profiler window and writes a text
// report (ProfilerDump.txt, next to the Assets folder): frame time stats, the markers that cost the most
// across the slowest frames, and the call tree of the very worst frames.
public static class ProfilerDump
{
    private const int WorstFrameCount = 8;    // frames whose call tree is written out
    private const int SpikeFrameCount = 40;   // slowest frames the totals are summed over
    private const float MinItemMs = 0.3f;     // smaller calls are left out of the trees
    private const int MaxDepth = 12;

    [MenuItem("Tools/Dump Profiler Spikes")]
    private static void Dump()
    {
        int first = ProfilerDriver.firstFrameIndex;
        int last = ProfilerDriver.lastFrameIndex;
        Debug.Log($"Profiler dump: frames {first} to {last} in the Profiler");
        if (first < 0 || last < first)
        {
            Debug.LogWarning("Profiler dump: no frames recorded. Record in the Profiler window, then run it before clearing.");
            EditorUtility.DisplayDialog("Profiler Dump", "No frames recorded. Record in the Profiler window first.", "OK");
            return;
        }

        // Every frame's time on the main thread
        var frameTimes = new List<(int frame, float ms)>();
        for (int frame = first; frame <= last; frame++)
        {
            using HierarchyFrameDataView view = GetView(frame);
            if (view != null && view.valid)
                frameTimes.Add((frame, view.frameTimeMs));
        }
        if (frameTimes.Count == 0)
        {
            Debug.LogWarning("Profiler dump: none of the recorded frames has CPU data on the main thread.");
            return;
        }

        var report = new StringBuilder();
        List<float> sorted = frameTimes.Select(f => f.ms).OrderBy(ms => ms).ToList();
        report.AppendLine($"Frames {first}-{last} ({frameTimes.Count} frames)");
        report.AppendLine($"Average {sorted.Average():F2} ms, median {sorted[sorted.Count / 2]:F2} ms, " +
                          $"95th percentile {sorted[(int)(sorted.Count * 0.95f)]:F2} ms, worst {sorted[^1]:F2} ms");
        report.AppendLine($"Frames over 16.7 ms: {sorted.Count(ms => ms > 16.7f)}, over 33 ms: {sorted.Count(ms => ms > 33f)}");
        report.AppendLine();

        var slowest = frameTimes.OrderByDescending(f => f.ms).ToList();

        // Self time and GC allocations per marker, summed over the slowest frames
        var selfTotals = new Dictionary<string, (float ms, float gc, int calls)>();
        foreach (var (frame, _) in slowest.Take(SpikeFrameCount))
        {
            using HierarchyFrameDataView view = GetView(frame);
            if (view == null || !view.valid) continue;
            SumSelf(view, view.GetRootItemID(), selfTotals);
        }
        int summed = Mathf.Min(SpikeFrameCount, slowest.Count);
        report.AppendLine($"== Biggest self time, summed over the {summed} slowest frames ==");
        foreach (var entry in selfTotals.OrderByDescending(e => e.Value.ms).Take(40))
            report.AppendLine($"{entry.Value.ms / summed,8:F2} ms/frame  {entry.Value.gc / summed / 1024f,8:F1} KB GC/frame  " +
                              $"{entry.Value.calls / (float)summed,7:F1} calls/frame  {entry.Key}");
        report.AppendLine();

        report.AppendLine("== Biggest GC allocations, summed over the same frames ==");
        foreach (var entry in selfTotals.Where(e => e.Value.gc > 0).OrderByDescending(e => e.Value.gc).Take(20))
            report.AppendLine($"{entry.Value.gc / summed / 1024f,8:F1} KB/frame  {entry.Key}");
        report.AppendLine();

        foreach (var (frame, ms) in slowest.Take(WorstFrameCount))
        {
            report.AppendLine($"== Frame {frame}: {ms:F2} ms ==");
            using HierarchyFrameDataView view = GetView(frame);
            if (view != null && view.valid)
                WriteTree(view, view.GetRootItemID(), 0, report);
            report.AppendLine();
        }

        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProfilerDump.txt"));
        File.WriteAllText(path, report.ToString());
        Debug.Log($"Profiler report written to {path}");
        EditorUtility.RevealInFinder(path);
    }

    private static HierarchyFrameDataView GetView(int frame) =>
        ProfilerDriver.GetHierarchyFrameDataView(frame, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                                                 HierarchyFrameDataView.columnTotalTime, false);

    private static void SumSelf(HierarchyFrameDataView view, int id, Dictionary<string, (float ms, float gc, int calls)> totals)
    {
        var children = new List<int>();
        view.GetItemChildren(id, children);
        foreach (int child in children)
        {
            string name = view.GetItemName(child);
            float self = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnSelfTime);
            float gc = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnGcMemory);
            int calls = (int)view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnCalls);

            totals.TryGetValue(name, out var total);
            // GC is reported including children, so only count it where it's allocated (no child allocates)
            totals[name] = (total.ms + self, total.gc + SelfGc(view, child, gc), total.calls + calls);
            SumSelf(view, child, totals);
        }
    }

    private static float SelfGc(HierarchyFrameDataView view, int id, float totalGc)
    {
        var children = new List<int>();
        view.GetItemChildren(id, children);
        float childGc = 0f;
        foreach (int child in children)
            childGc += view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnGcMemory);
        return Mathf.Max(0f, totalGc - childGc);
    }

    private static void WriteTree(HierarchyFrameDataView view, int id, int depth, StringBuilder report)
    {
        if (depth > MaxDepth) return;

        var children = new List<int>();
        view.GetItemChildren(id, children);
        foreach (int child in children)
        {
            float total = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnTotalTime);
            if (total < MinItemMs) continue;

            float self = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnSelfTime);
            float gc = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnGcMemory);
            int calls = (int)view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnCalls);
            report.AppendLine($"{new string(' ', depth * 2)}{view.GetItemName(child)}  total {total:F2} ms, self {self:F2} ms, " +
                              $"{calls} calls, GC {gc / 1024f:F1} KB");
            WriteTree(view, child, depth + 1, report);
        }
    }
}
