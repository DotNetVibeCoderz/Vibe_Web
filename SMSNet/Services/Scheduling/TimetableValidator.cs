namespace SMSNet.Services.Scheduling;

/// <summary>
/// Re-checks a timetable after a human has edited it.
/// <para>
/// The generator guarantees a clash-free result, but the whole point of the editor is
/// that a head teacher can override it. Every edit is therefore re-validated against
/// the same rules, so a manual change can never quietly reintroduce a clash.
/// </para>
/// </summary>
public sealed class TimetableValidator
{
    public IReadOnlyList<TimetableConflict> Validate(
        IReadOnlyList<ProposedLesson> lessons,
        TimetableRequest request)
    {
        var conflicts = new List<TimetableConflict>();

        conflicts.AddRange(FindDoubleBookings(lessons));
        conflicts.AddRange(FindTeacherProblems(lessons, request));
        conflicts.AddRange(FindPeriodMismatches(lessons, request));

        return conflicts;
    }

    /// <summary>Cell-level lookup, so the grid can highlight exactly which cells clash.</summary>
    public HashSet<(string ClassName, int Day, int Slot)> ConflictedCells(
        IReadOnlyList<ProposedLesson> lessons)
    {
        var cells = new HashSet<(string, int, int)>();

        // Two lessons for one class at one time.
        foreach (var group in lessons.GroupBy(l => (l.ClassName, l.DayIndex, l.SlotIndex)))
        {
            if (group.Count() > 1)
            {
                cells.Add(group.Key);
            }
        }

        // One teacher in two rooms at one time — every participating cell is marked,
        // because either of them could be the one to move.
        foreach (var group in lessons
                     .Where(l => !string.IsNullOrWhiteSpace(l.Teacher))
                     .GroupBy(l => (l.Teacher, l.DayIndex, l.SlotIndex)))
        {
            if (group.Count() > 1)
            {
                foreach (var lesson in group)
                {
                    cells.Add((lesson.ClassName, lesson.DayIndex, lesson.SlotIndex));
                }
            }
        }

        foreach (var lesson in lessons.Where(l => string.IsNullOrWhiteSpace(l.Teacher)))
        {
            cells.Add((lesson.ClassName, lesson.DayIndex, lesson.SlotIndex));
        }

        return cells;
    }

    private static IEnumerable<TimetableConflict> FindDoubleBookings(IReadOnlyList<ProposedLesson> lessons)
    {
        foreach (var group in lessons.GroupBy(l => (l.ClassName, l.Day, l.TimeSlot)))
        {
            if (group.Count() > 1)
            {
                var subjects = string.Join(" & ", group.Select(g => g.Subject).Distinct());
                yield return new TimetableConflict(
                    ConflictKind.ClassDoubleBooked,
                    $"Kelas {group.Key.ClassName} punya {group.Count()} pelajaran bersamaan " +
                    $"({subjects}) pada {group.Key.Day} {group.Key.TimeSlot}.",
                    group.Key.ClassName, group.Key.Day, group.Key.TimeSlot);
            }
        }

        foreach (var group in lessons
                     .Where(l => !string.IsNullOrWhiteSpace(l.Teacher))
                     .GroupBy(l => (l.Teacher, l.Day, l.TimeSlot)))
        {
            if (group.Count() > 1)
            {
                var classes = string.Join(" & ", group.Select(g => g.ClassName).Distinct());
                yield return new TimetableConflict(
                    ConflictKind.TeacherDoubleBooked,
                    $"{group.Key.Teacher} dijadwalkan di {group.Count()} kelas bersamaan " +
                    $"({classes}) pada {group.Key.Day} {group.Key.TimeSlot}.",
                    null, group.Key.Day, group.Key.TimeSlot, group.Key.Teacher);
            }
        }
    }

    private static IEnumerable<TimetableConflict> FindTeacherProblems(
        IReadOnlyList<ProposedLesson> lessons, TimetableRequest request)
    {
        foreach (var lesson in lessons.Where(l => string.IsNullOrWhiteSpace(l.Teacher)))
        {
            yield return new TimetableConflict(
                ConflictKind.NoTeacher,
                $"{lesson.ClassName} · {lesson.Subject} pada {lesson.Day} {lesson.TimeSlot} belum punya guru.",
                lesson.ClassName, lesson.Day, lesson.TimeSlot);
        }

        foreach (var lesson in lessons.Where(l => !string.IsNullOrWhiteSpace(l.Teacher)))
        {
            if (request.TeacherSubjects.TryGetValue(lesson.Teacher, out var subjects) &&
                !subjects.Contains(lesson.Subject, StringComparer.OrdinalIgnoreCase))
            {
                yield return new TimetableConflict(
                    ConflictKind.TeacherNotQualified,
                    $"{lesson.Teacher} tidak tercatat mengampu {lesson.Subject} " +
                    $"({lesson.ClassName}, {lesson.Day} {lesson.TimeSlot}).",
                    lesson.ClassName, lesson.Day, lesson.TimeSlot, lesson.Teacher);
            }
        }

        foreach (var group in lessons
                     .Where(l => !string.IsNullOrWhiteSpace(l.Teacher))
                     .GroupBy(l => (l.Teacher, l.Day)))
        {
            if (group.Count() > request.MaxPeriodsPerTeacherPerDay)
            {
                yield return new TimetableConflict(
                    ConflictKind.TeacherOverloaded,
                    $"{group.Key.Teacher} mengajar {group.Count()} jam pada {group.Key.Day} — " +
                    $"melebihi batas {request.MaxPeriodsPerTeacherPerDay} jam per hari.",
                    null, group.Key.Day, null, group.Key.Teacher);
            }
        }
    }

    private static IEnumerable<TimetableConflict> FindPeriodMismatches(
        IReadOnlyList<ProposedLesson> lessons, TimetableRequest request)
    {
        foreach (var className in request.Classes)
        {
            foreach (var (subject, expected) in request.SubjectPeriods.Where(s => s.Value > 0))
            {
                var actual = lessons.Count(l =>
                    l.ClassName == className &&
                    string.Equals(l.Subject, subject, StringComparison.OrdinalIgnoreCase));

                if (actual != expected)
                {
                    yield return new TimetableConflict(
                        ConflictKind.PeriodCountMismatch,
                        $"{className} punya {actual} jam {subject}, seharusnya {expected}.",
                        className);
                }
            }
        }
    }
}
