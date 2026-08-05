using System.Diagnostics;

namespace SMSNet.Services.Scheduling;

/// <summary>
/// Builds a clash-free timetable.
/// <para>
/// This is a constraint satisfaction problem, and it is solved as one: backtracking
/// search with the <b>minimum-remaining-values</b> heuristic (always place the lesson
/// with the fewest legal options next), <b>forward checking</b> (abandon a branch the
/// moment any unplaced lesson runs out of options), and randomised restarts to escape
/// a bad early commitment.
/// </para>
/// <para>
/// A plain greedy pass is much simpler but routinely paints itself into a corner on a
/// full timetable — it fills the easy slots first and then cannot place the hard ones.
/// Backtracking with MRV is the standard remedy and is comfortably fast at school
/// scale (a few hundred lessons).
/// </para>
/// </summary>
public sealed class TimetableGenerator
{
    /// <summary>Wall-clock ceiling. A school timetable solves in well under a second.</summary>
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(8);

    private const int MaxRestarts = 40;

    /// <summary>One lesson that still needs a day, slot, and teacher.</summary>
    private sealed class Demand
    {
        public required string ClassName { get; init; }
        public required string Subject { get; init; }
        public required List<string> EligibleTeachers { get; init; }

        /// <summary>Which repeat this is (0-based) of the subject's weekly periods.</summary>
        public int Occurrence { get; init; }
    }

    /// <summary>Occupancy during the search: who and what is busy when.</summary>
    private sealed class Board
    {
        private readonly HashSet<(string Class, int Day, int Slot)> _classBusy = new();
        private readonly HashSet<(string Teacher, int Day, int Slot)> _teacherBusy = new();
        private readonly Dictionary<(string Teacher, int Day), int> _teacherDayLoad = new();
        private readonly HashSet<(string Class, string Subject, int Day)> _subjectOnDay = new();

        public bool ClassFree(string cls, int day, int slot) => !_classBusy.Contains((cls, day, slot));

        public bool TeacherFree(string teacher, int day, int slot) => !_teacherBusy.Contains((teacher, day, slot));

        public bool TeacherHasRoom(string teacher, int day, int cap) =>
            _teacherDayLoad.GetValueOrDefault((teacher, day)) < cap;

        public bool SubjectAlreadyToday(string cls, string subject, int day) =>
            _subjectOnDay.Contains((cls, subject, day));

        public void Place(Demand demand, string teacher, int day, int slot)
        {
            _classBusy.Add((demand.ClassName, day, slot));
            _teacherBusy.Add((teacher, day, slot));
            _teacherDayLoad[(teacher, day)] = _teacherDayLoad.GetValueOrDefault((teacher, day)) + 1;
            _subjectOnDay.Add((demand.ClassName, demand.Subject, day));
        }

        public void Remove(Demand demand, string teacher, int day, int slot, bool wasOnlyOneToday)
        {
            _classBusy.Remove((demand.ClassName, day, slot));
            _teacherBusy.Remove((teacher, day, slot));

            var key = (teacher, day);
            var load = _teacherDayLoad.GetValueOrDefault(key) - 1;
            if (load <= 0) _teacherDayLoad.Remove(key); else _teacherDayLoad[key] = load;

            if (wasOnlyOneToday)
            {
                _subjectOnDay.Remove((demand.ClassName, demand.Subject, day));
            }
        }
    }

    public TimetableResult Generate(TimetableRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TimetableResult();

        var validation = Validate(request);
        if (validation is not null)
        {
            result.Summary = validation;
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }

        var demands = BuildDemands(request);

        if (demands.Count == 0)
        {
            result.Summary = "Tidak ada mata pelajaran yang perlu dijadwalkan.";
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }

        // Restart with a different random ordering when a run stalls: a poor early
        // choice can make an otherwise solvable timetable look impossible, and
        // restarting is far cheaper than exhausting that whole branch.
        for (var attempt = 1; attempt <= MaxRestarts && stopwatch.Elapsed < Budget; attempt++)
        {
            var random = new Random(request.Seed + attempt * 7919);
            var board = new Board();
            var placed = new List<ProposedLesson>(demands.Count);
            var remaining = new List<Demand>(demands);

            // First pass honours the "same subject once a day" preference; if the whole
            // search fails under it, later attempts drop it rather than give up.
            var honourSpread = request.AvoidSameSubjectTwiceADay && attempt <= MaxRestarts / 2;

            if (Solve(request, board, remaining, placed, random, honourSpread, stopwatch))
            {
                result.Lessons = placed
                    .OrderBy(l => l.ClassName)
                    .ThenBy(l => l.DayIndex)
                    .ThenBy(l => l.SlotIndex)
                    .ToList();

                result.Attempts = attempt;
                result.Elapsed = stopwatch.Elapsed;
                result.Summary =
                    $"Berhasil menyusun {result.Lessons.Count} jam pelajaran untuk " +
                    $"{request.Classes.Count} kelas tanpa bentrok" +
                    (honourSpread ? "." : ", dengan melonggarkan aturan satu mata pelajaran per hari.");
                return result;
            }

            result.Attempts = attempt;
        }

        // Out of budget: return the best partial fill so the operator can see what blocks.
        var partial = BestEffort(request, demands);
        result.Lessons = partial.Placed;
        result.Unplaced = partial.Unplaced;
        result.Elapsed = stopwatch.Elapsed;
        result.Summary =
            $"Hanya {partial.Placed.Count} dari {demands.Count} jam pelajaran yang dapat ditempatkan. " +
            $"{partial.Unplaced.Count} sisanya tidak muat — kurangi jam per mata pelajaran, " +
            "tambah guru pengampu, atau tambah slot waktu.";

        return result;
    }

    // --- Search ------------------------------------------------------------

    private static bool Solve(
        TimetableRequest request,
        Board board,
        List<Demand> remaining,
        List<ProposedLesson> placed,
        Random random,
        bool honourSpread,
        Stopwatch stopwatch)
    {
        if (remaining.Count == 0)
        {
            return true;
        }

        if (stopwatch.Elapsed > Budget)
        {
            return false;
        }

        // MRV: take the lesson with the fewest legal placements. Failing fast on the
        // hardest lesson prunes far more of the tree than working in list order.
        var bestIndex = -1;
        List<(string Teacher, int Day, int Slot)>? bestOptions = null;

        for (var i = 0; i < remaining.Count; i++)
        {
            var options = LegalPlacements(request, board, remaining[i], honourSpread);

            if (options.Count == 0)
            {
                return false;   // forward checking: this branch is already dead
            }

            if (bestOptions is null || options.Count < bestOptions.Count)
            {
                bestIndex = i;
                bestOptions = options;

                if (options.Count == 1)
                {
                    break;      // forced move — no better candidate exists
                }
            }
        }

        var demand = remaining[bestIndex];
        remaining.RemoveAt(bestIndex);

        // Shuffle so restarts explore genuinely different shapes.
        Shuffle(bestOptions!, random);

        foreach (var (teacher, day, slot) in bestOptions!)
        {
            var firstToday = !board.SubjectAlreadyToday(demand.ClassName, demand.Subject, day);

            board.Place(demand, teacher, day, slot);
            placed.Add(new ProposedLesson
            {
                ClassName = demand.ClassName,
                Subject = demand.Subject,
                Teacher = teacher,
                Day = request.Days[day],
                TimeSlot = request.TimeSlots[slot],
                DayIndex = day,
                SlotIndex = slot
            });

            if (Solve(request, board, remaining, placed, random, honourSpread, stopwatch))
            {
                return true;
            }

            placed.RemoveAt(placed.Count - 1);
            board.Remove(demand, teacher, day, slot, firstToday);
        }

        remaining.Insert(bestIndex, demand);
        return false;
    }

    private static List<(string Teacher, int Day, int Slot)> LegalPlacements(
        TimetableRequest request, Board board, Demand demand, bool honourSpread)
    {
        var options = new List<(string, int, int)>();

        for (var day = 0; day < request.Days.Count; day++)
        {
            if (honourSpread && board.SubjectAlreadyToday(demand.ClassName, demand.Subject, day))
            {
                continue;
            }

            for (var slot = 0; slot < request.TimeSlots.Count; slot++)
            {
                if (!board.ClassFree(demand.ClassName, day, slot))
                {
                    continue;
                }

                foreach (var teacher in demand.EligibleTeachers)
                {
                    if (board.TeacherFree(teacher, day, slot) &&
                        board.TeacherHasRoom(teacher, day, request.MaxPeriodsPerTeacherPerDay))
                    {
                        options.Add((teacher, day, slot));
                    }
                }
            }
        }

        return options;
    }

    /// <summary>
    /// Greedy fill used only when the search runs out of budget — it shows how far the
    /// timetable gets and which lessons are the obstruction.
    /// </summary>
    private static (List<ProposedLesson> Placed, List<string> Unplaced) BestEffort(
        TimetableRequest request, List<Demand> demands)
    {
        var board = new Board();
        var placed = new List<ProposedLesson>();
        var unplaced = new List<string>();

        // Hardest first, so the report names the genuinely impossible lessons.
        foreach (var demand in demands.OrderBy(d => d.EligibleTeachers.Count))
        {
            var options = LegalPlacements(request, board, demand, request.AvoidSameSubjectTwiceADay);

            if (options.Count == 0)
            {
                options = LegalPlacements(request, board, demand, false);
            }

            if (options.Count == 0)
            {
                unplaced.Add($"{demand.ClassName} · {demand.Subject} (jam ke-{demand.Occurrence + 1})");
                continue;
            }

            var (teacher, day, slot) = options[0];
            board.Place(demand, teacher, day, slot);

            placed.Add(new ProposedLesson
            {
                ClassName = demand.ClassName,
                Subject = demand.Subject,
                Teacher = teacher,
                Day = request.Days[day],
                TimeSlot = request.TimeSlots[slot],
                DayIndex = day,
                SlotIndex = slot
            });
        }

        return (placed.OrderBy(l => l.ClassName).ThenBy(l => l.DayIndex).ThenBy(l => l.SlotIndex).ToList(),
                unplaced);
    }

    // --- Setup -------------------------------------------------------------

    private static List<Demand> BuildDemands(TimetableRequest request)
    {
        var demands = new List<Demand>();

        foreach (var className in request.Classes)
        {
            foreach (var (subject, periods) in request.SubjectPeriods)
            {
                if (periods <= 0)
                {
                    continue;
                }

                var teachers = request.TeacherSubjects
                    .Where(kv => kv.Value.Contains(subject, StringComparer.OrdinalIgnoreCase))
                    .Select(kv => kv.Key)
                    .ToList();

                for (var occurrence = 0; occurrence < periods; occurrence++)
                {
                    demands.Add(new Demand
                    {
                        ClassName = className,
                        Subject = subject,
                        EligibleTeachers = teachers,
                        Occurrence = occurrence
                    });
                }
            }
        }

        return demands;
    }

    /// <summary>Catches the impossible requests up front, with an explanation an operator can act on.</summary>
    private static string? Validate(TimetableRequest request)
    {
        if (request.Classes.Count == 0)
        {
            return "Belum ada kelas. Tambahkan kelas pada Master Data terlebih dahulu.";
        }

        if (request.SubjectPeriods.Count == 0 || request.SubjectPeriods.Values.All(v => v <= 0))
        {
            return "Belum ada mata pelajaran dengan jam per minggu lebih dari nol.";
        }

        if (request.Days.Count == 0 || request.TimeSlots.Count == 0)
        {
            return "Hari dan slot waktu tidak boleh kosong.";
        }

        var perClass = request.SubjectPeriods.Values.Sum();
        var slotsPerClass = request.Days.Count * request.TimeSlots.Count;

        if (perClass > slotsPerClass)
        {
            return $"Total {perClass} jam per minggu melebihi {slotsPerClass} slot yang tersedia " +
                   $"({request.Days.Count} hari × {request.TimeSlots.Count} slot). " +
                   "Kurangi jam pelajaran atau tambah slot waktu.";
        }

        foreach (var subject in request.SubjectPeriods.Where(s => s.Value > 0).Select(s => s.Key))
        {
            var teacherCount = request.TeacherSubjects
                .Count(kv => kv.Value.Contains(subject, StringComparer.OrdinalIgnoreCase));

            if (teacherCount == 0)
            {
                return $"Tidak ada guru yang mengampu \"{subject}\". " +
                       "Tetapkan pengampunya pada Master Data → Guru, atau setel jamnya ke nol.";
            }

            // A subject needs enough teacher-time to cover every class that studies it.
            var demand = request.SubjectPeriods[subject] * request.Classes.Count;
            var supply = teacherCount * request.Days.Count * request.TimeSlots.Count;

            if (demand > supply)
            {
                return $"\"{subject}\" membutuhkan {demand} jam, tetapi {teacherCount} guru pengampunya " +
                       $"hanya menyediakan {supply} slot. Tambah guru pengampu atau kurangi jamnya.";
            }
        }

        return null;
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
