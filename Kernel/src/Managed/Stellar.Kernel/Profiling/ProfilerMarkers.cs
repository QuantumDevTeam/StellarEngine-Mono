using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Stellar.Kernel.Profiling;

/// <summary>
/// The names of the markers and counters of the process, by number: an event carries the number, a profiler reads the
/// name here.
/// </summary>
/// <remarks>
/// <para>A name is description, not state: like a type, it is the same for every engine of the process, so the table is
/// one for the process. What is recorded belongs to the session of each engine.</para>
/// <para>A name is registered once and keeps its number; registering it again gives the same number. Number 0 is no
/// marker.</para>
/// </remarks>
public static class ProfilerMarkers
{
    private static readonly ConcurrentDictionary<string, int> Numbers = new(StringComparer.Ordinal);
    private static readonly object Gate = new();

    // Replaced on change, never mutated: readers need no lock
    private static Entry[] _entries = [new Entry(string.Empty, ProfilerCategory.Other)];

    /// <summary>
    /// Gets how many numbers are given, with 0.
    /// </summary>
    public static int Count => Volatile.Read(ref _entries).Length;

    /// <summary>
    /// Raised when a name gets a number: a profiler that runs tells its viewer.
    /// </summary>
    public static event Action<int, string, ProfilerCategory>? Registered;

    /// <summary>
    /// Gives the number of a name, registering it the first time.
    /// </summary>
    /// <param name="name">The name; by convention dotted, from the general to the precise: <c>Physics.Step</c>.</param>
    /// <param name="category">Where it belongs; the category of its first registration stays.</param>
    /// <returns>The number, from 1.</returns>
    public static int Register(string name, ProfilerCategory category = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A marker has a name", nameof(name));
        if (Numbers.TryGetValue(name, out var known)) return known;

        int number;
        lock (Gate)
        {
            if (Numbers.TryGetValue(name, out known)) return known;

            var entries = _entries;
            number = entries.Length;
            Volatile.Write(ref _entries, [.. entries, new Entry(name, category)]);
            Numbers[name] = number;
        }

        Registered?.Invoke(number, name, category);
        return number;
    }

    /// <summary>
    /// Gets the name of a number.
    /// </summary>
    public static string NameOf(int number)
    {
        return At(number).Name;
    }

    /// <summary>
    /// Gets the category of a number.
    /// </summary>
    public static ProfilerCategory CategoryOf(int number)
    {
        return At(number).Category;
    }

    /// <summary>
    /// Gets every name with its number and its category, in the order of the numbers: what a profiler writes into a
    /// capture.
    /// </summary>
    public static IReadOnlyList<(int Number, string Name, ProfilerCategory Category)> All()
    {
        var entries = Volatile.Read(ref _entries);
        var all = new (int, string, ProfilerCategory)[entries.Length - 1];
        for (var i = 1; i < entries.Length; i++) all[i - 1] = (i, entries[i].Name, entries[i].Category);
        return all;
    }

    private static Entry At(int number)
    {
        var entries = Volatile.Read(ref _entries);
        return (uint)number < (uint)entries.Length
            ? entries[number]
            : throw new ArgumentOutOfRangeException(nameof(number), number, "No marker has this number");
    }

    private sealed record Entry(string Name, ProfilerCategory Category);
}
