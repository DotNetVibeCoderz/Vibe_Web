namespace SMSNet.Services.Scheduling;

/// <summary>One placed lesson in a proposed timetable.</summary>
public sealed class ProposedLesson
{
    public required string ClassName { get; set; }
    public required string Subject { get; set; }
    public required string Teacher { get; set; }
    public required string Day { get; set; }
    public required string TimeSlot { get; set; }

    /// <summary>Grid position, so the editor can address a cell without string parsing.</summary>
    public int DayIndex { get; set; }
    public int SlotIndex { get; set; }

    public ProposedLesson Clone() => new()
    {
        ClassName = ClassName, Subject = Subject, Teacher = Teacher,
        Day = Day, TimeSlot = TimeSlot, DayIndex = DayIndex, SlotIndex = SlotIndex
    };
}

/// <summary>What the generator was asked to produce.</summary>
public sealed class TimetableRequest
{
    public List<string> Days { get; set; } = new() { "Senin", "Selasa", "Rabu", "Kamis", "Jumat" };

    public List<string> TimeSlots { get; set; } = new()
    {
        "07:00 - 07:40", "07:40 - 08:20", "08:20 - 09:00",
        "09:20 - 10:00", "10:00 - 10:40", "10:40 - 11:20",
        "12:30 - 13:10", "13:10 - 13:50"
    };

    /// <summary>Classes to schedule, by name.</summary>
    public List<string> Classes { get; set; } = new();

    /// <summary>Subject → periods per week for every class.</summary>
    public Dictionary<string, int> SubjectPeriods { get; set; } = new();

    /// <summary>Teacher → the subjects they are qualified to teach.</summary>
    public Dictionary<string, List<string>> TeacherSubjects { get; set; } = new();

    /// <summary>Ceiling on how many periods one teacher may take in a single day.</summary>
    public int MaxPeriodsPerTeacherPerDay { get; set; } = 6;

    /// <summary>
    /// Try to keep a class from meeting the same subject twice in one day. A soft
    /// preference — the solver relaxes it rather than failing outright.
    /// </summary>
    public bool AvoidSameSubjectTwiceADay { get; set; } = true;

    /// <summary>Fixes the random ordering so a run can be reproduced or deliberately re-rolled.</summary>
    public int Seed { get; set; } = Environment.TickCount;

    public int TotalDemand => Classes.Count * SubjectPeriods.Values.Sum();

    public int Capacity => Classes.Count * Days.Count * TimeSlots.Count;
}

public enum ConflictKind
{
    /// <summary>One teacher is in two places at the same time.</summary>
    TeacherDoubleBooked,
    /// <summary>One class has two lessons at the same time.</summary>
    ClassDoubleBooked,
    /// <summary>The teacher is not recorded as teaching that subject.</summary>
    TeacherNotQualified,
    /// <summary>A lesson has no teacher assigned.</summary>
    NoTeacher,
    /// <summary>The class has more or fewer periods of a subject than requested.</summary>
    PeriodCountMismatch,
    /// <summary>The teacher exceeds the daily period cap.</summary>
    TeacherOverloaded
}

public sealed record TimetableConflict(
    ConflictKind Kind,
    string Message,
    string? ClassName = null,
    string? Day = null,
    string? TimeSlot = null,
    string? Teacher = null)
{
    /// <summary>
    /// Whether this must be resolved before saving. Overload and period-count issues
    /// are worth flagging but do not make a timetable unusable — a head teacher may
    /// knowingly accept them.
    /// </summary>
    public bool IsBlocking => Kind is ConflictKind.TeacherDoubleBooked
        or ConflictKind.ClassDoubleBooked
        or ConflictKind.NoTeacher;
}

/// <summary>What the generator produced, and how well it did.</summary>
public sealed class TimetableResult
{
    public List<ProposedLesson> Lessons { get; set; } = new();

    /// <summary>Lessons the solver could not place at all.</summary>
    public List<string> Unplaced { get; set; } = new();

    public bool Success => Unplaced.Count == 0;

    public int Attempts { get; set; }

    public TimeSpan Elapsed { get; set; }

    /// <summary>Explains the outcome in the operator's terms, not the solver's.</summary>
    public string Summary { get; set; } = string.Empty;
}
