using System;
using System.Collections.Generic;
using TimeFold.Core.Settings;

namespace TimeFold.Core.Naming
{
    // The base layouts of the naming template; "flipped" and "short month" turn each one into a FolderFormat.
    public enum CoreFormat { YearMonth, IsoMonth, Daily, IsoDate, YearNestedDaily, YearNestedIsoDaily, YearQuarter, YearQuarterMonths, YearHalf, YearOnly, YearNestedMonth, YearNestedMonthOnly, YearNestedIso, YearNestedQuarter, YearNestedHalf }

    public record DateFormatOption(string Group, CoreFormat Core);

    /// <summary>
    /// The naming template logic that lived inside PreferencesForm: the grouped list, the display names,
    /// which modifiers apply, and the round trip between (core, flipped, short month) and FolderFormat.
    /// </summary>
    public static class DateFormatChoice
    {
        public static readonly IReadOnlyList<DateFormatOption> Options =
        [
            new("By Year", CoreFormat.YearOnly),

            new("By Month", CoreFormat.YearMonth),
            new("By Month", CoreFormat.IsoMonth),
            new("By Month", CoreFormat.YearNestedMonthOnly),
            new("By Month", CoreFormat.YearNestedMonth),
            new("By Month", CoreFormat.YearNestedIso),

            new("By Day", CoreFormat.Daily),
            new("By Day", CoreFormat.IsoDate),
            new("By Day", CoreFormat.YearNestedDaily),
            new("By Day", CoreFormat.YearNestedIsoDaily),

            new("By Quarter & Half-Year", CoreFormat.YearQuarter),
            new("By Quarter & Half-Year", CoreFormat.YearQuarterMonths),
            new("By Quarter & Half-Year", CoreFormat.YearNestedQuarter),
            new("By Quarter & Half-Year", CoreFormat.YearHalf),
            new("By Quarter & Half-Year", CoreFormat.YearNestedHalf),
        ];

        public static string GetDisplayName(CoreFormat core, bool flipped, bool shortMonth, int year)
        {
            string m = shortMonth ? "Mar" : "March";
            string qm = shortMonth ? "Jan, Feb & Mar" : "January, February & March";
            return core switch
            {
                CoreFormat.YearMonth => flipped ? $"Month & Year (e.g. {m} {year})" : $"Year & Month (e.g. {year} {m})",
                CoreFormat.IsoMonth => flipped ? $"ISO 8601 (e.g. 03-{year})" : $"ISO 8601 (e.g. {year}-03)",
                CoreFormat.Daily => flipped ? $"Day, Month & Year (e.g. 01 {m} {year})" : $"Year, Month & Day (e.g. {year} {m} 01)",
                CoreFormat.IsoDate => flipped ? $"ISO Date (e.g. 01-03-{year})" : $"ISO Date (e.g. {year}-03-01)",
                CoreFormat.YearNestedDaily => flipped ? $"Day / Month / Year (e.g. 01 \\ {m} \\ {year})" : $"Year / Month / Day (e.g. {year} \\ {m} \\ 01)",
                CoreFormat.YearNestedIsoDaily => flipped ? $"Day / ISO Month / Year (e.g. 01 \\ 03-{year} \\ {year})" : $"Year / ISO Month / Day (e.g. {year} \\ {year}-03 \\ 01)",
                CoreFormat.YearQuarter => flipped ? $"Quarter & Year (e.g. Q1 {year})" : $"Year & Quarter (e.g. {year} Q1)",
                CoreFormat.YearQuarterMonths => $"Year & Quarter with Months (e.g. {year} Q1 ({qm}))",
                CoreFormat.YearHalf => flipped ? $"Half & Year (e.g. H1 {year})" : $"Year & Half (e.g. {year} H1)",
                CoreFormat.YearOnly => $"Year (e.g. {year})",
                CoreFormat.YearNestedMonth => shortMonth ? $"Year / Year & Month (e.g. {year} \\ {year} Mar)" : $"Year / Year & Month (e.g. {year} \\ {year} {m})",
                CoreFormat.YearNestedMonthOnly => shortMonth ? $"Year / Month (e.g. {year} \\ Mar)" : $"Year / Month (e.g. {year} \\ {m})",
                CoreFormat.YearNestedIso => $"Year / ISO Month (e.g. {year} \\ {year}-03)",
                CoreFormat.YearNestedQuarter => flipped ? $"Year / Quarter & Year (e.g. {year} \\ Q1 {year})" : $"Year / Year & Quarter (e.g. {year} \\ {year} Q1)",
                CoreFormat.YearNestedHalf => flipped ? $"Year / Half & Year (e.g. {year} \\ H1 {year})" : $"Year / Year & Half (e.g. {year} \\ {year} H1)",
                _ => ""
            };
        }

        public static bool CanFlip(CoreFormat core) =>
            core is CoreFormat.YearMonth or CoreFormat.IsoMonth or CoreFormat.Daily or CoreFormat.IsoDate
                or CoreFormat.YearNestedDaily or CoreFormat.YearNestedIsoDaily
                or CoreFormat.YearQuarter or CoreFormat.YearHalf
                or CoreFormat.YearNestedMonth or CoreFormat.YearNestedIso
                or CoreFormat.YearNestedQuarter or CoreFormat.YearNestedHalf;

        public static bool CanShortMonth(CoreFormat core) =>
            core is CoreFormat.YearMonth or CoreFormat.Daily or CoreFormat.YearNestedDaily
                or CoreFormat.YearQuarterMonths or CoreFormat.YearNestedMonth or CoreFormat.YearNestedMonthOnly;

        public static FolderFormat Resolve(CoreFormat core, bool isFlipped, bool useShortMonth) => core switch
        {
            CoreFormat.YearMonth => (isFlipped, useShortMonth) switch
            {
                (false, false) => FolderFormat.YearMonth,
                (true, false) => FolderFormat.MonthYear,
                (false, true) => FolderFormat.YearShortMonth,
                (true, true) => FolderFormat.ShortMonthYear
            },
            CoreFormat.IsoMonth => isFlipped ? FolderFormat.MonthIso : FolderFormat.IsoMonth,
            CoreFormat.Daily => (isFlipped, useShortMonth) switch
            {
                (false, false) => FolderFormat.YearMonthDay,
                (true, false) => FolderFormat.DayMonthYear,
                (false, true) => FolderFormat.YearShortMonthDay,
                (true, true) => FolderFormat.DayShortMonthYear
            },
            CoreFormat.IsoDate => isFlipped ? FolderFormat.IsoDateFlipped : FolderFormat.IsoDate,
            CoreFormat.YearNestedDaily => (isFlipped, useShortMonth) switch
            {
                (false, false) => FolderFormat.YearWithMonthAndDay,
                (true, false) => FolderFormat.YearWithMonthAndDayFlipped,
                (false, true) => FolderFormat.YearWithShortMonthAndDay,
                (true, true) => FolderFormat.YearWithShortMonthAndDayFlipped
            },
            CoreFormat.YearNestedIsoDaily => isFlipped ? FolderFormat.YearWithIsoMonthAndDayFlipped : FolderFormat.YearWithIsoMonthAndDay,
            CoreFormat.YearQuarter => isFlipped ? FolderFormat.QuarterYear : FolderFormat.YearQuarter,
            CoreFormat.YearQuarterMonths => useShortMonth ? FolderFormat.YearQuarterShortMonths : FolderFormat.YearQuarterMonths,
            CoreFormat.YearHalf => isFlipped ? FolderFormat.HalfYear : FolderFormat.YearHalf,
            CoreFormat.YearOnly => FolderFormat.YearOnly,
            CoreFormat.YearNestedMonth => (isFlipped, useShortMonth) switch
            {
                (false, false) => FolderFormat.YearWithMonth,
                (true, false) => FolderFormat.YearWithMonthFlipped,
                (false, true) => FolderFormat.YearWithShortMonth,
                (true, true) => FolderFormat.YearWithShortMonthFlipped
            },
            CoreFormat.YearNestedMonthOnly => useShortMonth ? FolderFormat.YearWithShortMonthOnly : FolderFormat.YearWithMonthOnly,
            CoreFormat.YearNestedIso => isFlipped ? FolderFormat.YearWithIsoMonthFlipped : FolderFormat.YearWithIsoMonth,
            CoreFormat.YearNestedQuarter => isFlipped ? FolderFormat.YearWithQuarterFlipped : FolderFormat.YearWithQuarter,
            CoreFormat.YearNestedHalf => isFlipped ? FolderFormat.YearWithHalfFlipped : FolderFormat.YearWithHalf,
            _ => FolderFormat.YearMonth
        };

        /// <summary>
        /// The reverse of <see cref="Resolve"/>. The old form only recovered the core and always opened with
        /// flip and short month off, so saving without touching them reset a flipped format; this keeps them.
        /// </summary>
        public static (CoreFormat Core, bool IsFlipped, bool UseShortMonth) Split(FolderFormat format) => format switch
        {
            FolderFormat.YearMonth => (CoreFormat.YearMonth, false, false),
            FolderFormat.MonthYear => (CoreFormat.YearMonth, true, false),
            FolderFormat.YearShortMonth => (CoreFormat.YearMonth, false, true),
            FolderFormat.ShortMonthYear => (CoreFormat.YearMonth, true, true),
            FolderFormat.IsoMonth => (CoreFormat.IsoMonth, false, false),
            FolderFormat.MonthIso => (CoreFormat.IsoMonth, true, false),
            FolderFormat.YearMonthDay => (CoreFormat.Daily, false, false),
            FolderFormat.DayMonthYear => (CoreFormat.Daily, true, false),
            FolderFormat.YearShortMonthDay => (CoreFormat.Daily, false, true),
            FolderFormat.DayShortMonthYear => (CoreFormat.Daily, true, true),
            FolderFormat.IsoDate => (CoreFormat.IsoDate, false, false),
            FolderFormat.IsoDateFlipped => (CoreFormat.IsoDate, true, false),
            FolderFormat.YearWithMonthAndDay => (CoreFormat.YearNestedDaily, false, false),
            FolderFormat.YearWithMonthAndDayFlipped => (CoreFormat.YearNestedDaily, true, false),
            FolderFormat.YearWithShortMonthAndDay => (CoreFormat.YearNestedDaily, false, true),
            FolderFormat.YearWithShortMonthAndDayFlipped => (CoreFormat.YearNestedDaily, true, true),
            FolderFormat.YearWithIsoMonthAndDay => (CoreFormat.YearNestedIsoDaily, false, false),
            FolderFormat.YearWithIsoMonthAndDayFlipped => (CoreFormat.YearNestedIsoDaily, true, false),
            FolderFormat.YearQuarter => (CoreFormat.YearQuarter, false, false),
            FolderFormat.QuarterYear => (CoreFormat.YearQuarter, true, false),
            FolderFormat.YearQuarterMonths => (CoreFormat.YearQuarterMonths, false, false),
            FolderFormat.YearQuarterShortMonths => (CoreFormat.YearQuarterMonths, false, true),
            FolderFormat.YearHalf => (CoreFormat.YearHalf, false, false),
            FolderFormat.HalfYear => (CoreFormat.YearHalf, true, false),
            FolderFormat.YearOnly => (CoreFormat.YearOnly, false, false),
            FolderFormat.YearWithMonth => (CoreFormat.YearNestedMonth, false, false),
            FolderFormat.YearWithMonthFlipped => (CoreFormat.YearNestedMonth, true, false),
            FolderFormat.YearWithShortMonth => (CoreFormat.YearNestedMonth, false, true),
            FolderFormat.YearWithShortMonthFlipped => (CoreFormat.YearNestedMonth, true, true),
            FolderFormat.YearWithMonthOnly => (CoreFormat.YearNestedMonthOnly, false, false),
            FolderFormat.YearWithShortMonthOnly => (CoreFormat.YearNestedMonthOnly, false, true),
            FolderFormat.YearWithIsoMonth => (CoreFormat.YearNestedIso, false, false),
            FolderFormat.YearWithIsoMonthFlipped => (CoreFormat.YearNestedIso, true, false),
            FolderFormat.YearWithQuarter => (CoreFormat.YearNestedQuarter, false, false),
            FolderFormat.YearWithQuarterFlipped => (CoreFormat.YearNestedQuarter, true, false),
            FolderFormat.YearWithHalf => (CoreFormat.YearNestedHalf, false, false),
            FolderFormat.YearWithHalfFlipped => (CoreFormat.YearNestedHalf, true, false),
            _ => (CoreFormat.YearMonth, false, false)
        };

        /// <summary>Four dates that show how the chosen layout spreads files out (the live preview).</summary>
        public static DateTime[] SampleDates(CoreFormat core, int currentYear) => core switch
        {
            CoreFormat.Daily or CoreFormat.IsoDate or CoreFormat.YearNestedDaily or CoreFormat.YearNestedIsoDaily =>
                [new(currentYear, 3, 1), new(currentYear, 3, 2), new(currentYear, 3, 3), new(currentYear, 3, 4)],
            CoreFormat.YearQuarter or CoreFormat.YearQuarterMonths or CoreFormat.YearNestedQuarter =>
                [new(currentYear, 2, 1), new(currentYear, 5, 1), new(currentYear, 8, 1), new(currentYear, 11, 1)],
            CoreFormat.YearHalf or CoreFormat.YearNestedHalf =>
                [new(currentYear - 1, 3, 1), new(currentYear - 1, 9, 1), new(currentYear, 3, 1), new(currentYear, 9, 1)],
            CoreFormat.YearOnly =>
                [new(currentYear - 3, 1, 1), new(currentYear - 2, 1, 1), new(currentYear - 1, 1, 1), new(currentYear, 1, 1)],
            _ =>
                [new(currentYear, 1, 15), new(currentYear, 2, 15), new(currentYear, 3, 15), new(currentYear, 4, 15)]
        };
    }
}
