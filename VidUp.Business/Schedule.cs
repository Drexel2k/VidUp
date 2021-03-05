using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Schedule
    {
        [JsonProperty]
        private ScheduleFrequency scheduleFrequency;
        [JsonProperty]
        private DateTime? ignoreUploadsBefore;
        [JsonProperty]
        private DateTime startDate;
        [JsonProperty]
        private DateTime uploadedUntil;

        [JsonProperty]
        private int dailyDayFrequency;
        [JsonProperty]
        private TimeSpan dailyDefaultTime;
        [JsonProperty]
        private bool dailyHasAdvancedSchedule;
        [JsonProperty]
        private bool[] dailyDays;
        [JsonProperty]
        private Dictionary<int, TimeSpan?[]> dailyDayTimes;

        [JsonProperty]
        private int weeklyWeekFrequency;
        [JsonProperty]
        private TimeSpan weeklyDefaultTime;
        [JsonProperty]
        private bool[] weeklyDays;
        [JsonProperty]
        private bool weeklyHasAdvancedSchedule;
        [JsonProperty]
        private Dictionary<DayOfWeek, TimeSpan?[]> weeklyDayTimes;

        [JsonProperty]
        private int monthlyMonthFrequency;
        [JsonProperty]
        private TimeSpan monthlyDefaultTime;

        [JsonProperty]
        private bool monthlyMonthDateBased;
        [JsonProperty]
        private bool[] monthlyMonthDateBasedDates;

        [JsonProperty]
        private List<MonthRelativeCombination> monthlyMonthRelativeBasedCombinations;

        [JsonProperty]
        private bool monthlyHasAdvancedSchedule;

        [JsonProperty]
        private Dictionary<int, TimeSpan?[]> monthlyMonthDateBasedDayTimes;
        private Dictionary<MonthRelativeCombination, TimeSpan?[]> monthlyMonthRelativeBasedDayTimes;

        [JsonProperty(PropertyName = "monthlyMonthRelativeBasedDayTimes")]
        private List<KeyValuePair<MonthRelativeCombination, TimeSpan?[]>> monthlyMonthRelativeBasedDayTimesForSerialization
        {
            get
            {
                //forcing the deserializer to the setter, otherwise it would append values.
                if (this.monthlyMonthRelativeBasedDayTimes == null)
                {
                    return null;
                }

                return this.monthlyMonthRelativeBasedDayTimes.ToList();
            }
            set
            {
                this.monthlyMonthRelativeBasedDayTimes = new Dictionary<MonthRelativeCombination, TimeSpan?[]>();
                foreach (KeyValuePair<MonthRelativeCombination, TimeSpan?[]> monthRelativeCombinationKeyValuePair in value)
                {
                    MonthRelativeCombination monthRelativeCombination =
                        this.monthlyMonthRelativeBasedCombinations.Find(mrc =>
                            mrc.DayPosition == monthRelativeCombinationKeyValuePair.Key.DayPosition &&
                            mrc.DayOfWeek == monthRelativeCombinationKeyValuePair.Key.DayOfWeek);

                    if (monthRelativeCombination == null)
                    {
                        monthRelativeCombination = new MonthRelativeCombination(monthRelativeCombinationKeyValuePair.Key.DayPosition, monthRelativeCombinationKeyValuePair.Key.DayOfWeek);
                    }

                    this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombination, new TimeSpan?[3]);

                    for (int index = 0; index < 3; index++)
                    {
                        TimeSpan? timeSpan = monthRelativeCombinationKeyValuePair.Value[index];
                        this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][index] = null;
                        if (timeSpan != null)
                        {
                            this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][index] = new TimeSpan(timeSpan.Value.Hours, timeSpan.Value.Minutes, timeSpan.Value.Seconds);
                        }
                    }
                }
            }
        }

        [OnDeserializing()]
        private void OnDeserializingMethod(StreamingContext context)
        {
            this.monthlyMonthRelativeBasedDayTimes = null;
            this.monthlyMonthRelativeBasedCombinations.Clear();
        }
        public ScheduleFrequency ScheduleFrequency
        {
            get => this.scheduleFrequency;
            set => this.scheduleFrequency = value;
        }
        public DateTime? IgnoreUploadsBefore
        {
            get => this.ignoreUploadsBefore;
            set => this.ignoreUploadsBefore = value;
        }

        public DateTime StartDate
        {
            get => this.startDate;
            set
            {
                this.startDate = value;
                this.uploadedUntil = DateTime.MinValue;
            }
        }

        public DateTime UploadedUntil
        {
            get => this.uploadedUntil;
            set => this.uploadedUntil = value;
        }

        public int DailyDayFrequency
        {
            get => this.dailyDayFrequency;
            set => this.dailyDayFrequency = value;
        }

        public bool DailyHasAdvancedSchedule
        {
            get => this.dailyHasAdvancedSchedule;
            set => this.dailyHasAdvancedSchedule = value;
        }

        public TimeSpan DailyDefaultTime
        {
            get => this.dailyDefaultTime;
            set => this.dailyDefaultTime = value;
        }

        public int WeeklyWeekFrequency
        {
            get => this.weeklyWeekFrequency;
            set => this.weeklyWeekFrequency = value;
        }

        public bool WeeklyHasAdvancedSchedule
        {
            get => this.weeklyHasAdvancedSchedule;
            set
            {
                if (value)
                {
                    this.weeklyCheckDayTimes();
                }

                this.weeklyHasAdvancedSchedule = value;
            }
        }

        public TimeSpan WeeklyDefaultTime
        {
            get => this.weeklyDefaultTime;
            set => this.weeklyDefaultTime = value;
        }

        public int MonthlyMonthFrequency
        {
            get => this.monthlyMonthFrequency;
            set => this.monthlyMonthFrequency = value;
        }

        public TimeSpan MonthlyDefaultTime
        {
            get => this.monthlyDefaultTime;
            set => this.monthlyDefaultTime = value;
        }

        public bool MonthlyDateBased
        {
            get => this.monthlyMonthDateBased;
            set
            {
                if (this.monthlyHasAdvancedSchedule)
                {
                    if (value)
                    {
                        this.monthlyMonthDateBasedCheckDayTimes();
                    }
                    else
                    {
                        this.monthlyMonthRelativeBasedCheckDayTimes();
                    }
                }

                this.monthlyMonthDateBased = value;
            }
        }

        public bool MonthlyHasAdvancedSchedule
        {
            get => this.monthlyHasAdvancedSchedule;
            set
            {
                if (value)
                {
                    if (this.monthlyMonthDateBased)
                    {
                        this.monthlyMonthDateBasedCheckDayTimes();
                    }
                    else
                    {
                        this.monthlyMonthRelativeBasedCheckDayTimes();
                    }
                }

                this.monthlyHasAdvancedSchedule = value;
            }
        }

        public int[] DailyDaysWithDayTimes
        {
            get => this.dailyDayTimes.Keys.ToArray();
        }

        public DayOfWeek[] WeeklyDaysWithDayTimes
        {
            get => this.weeklyDayTimes.Keys.ToArray();
        }

        public ReadOnlyCollection<MonthRelativeCombination> MonthlyMonthRelativeBasedCombinations
        {
            get => this.monthlyMonthRelativeBasedCombinations.AsReadOnly();
        }

        public MonthRelativeCombination[] MonthlyMonthRelativeBasedCombinationsWithDayTimes
        {
            get => this.monthlyMonthRelativeBasedDayTimes.Keys.ToArray();
        }

        public int[] MonthlyMonthDateBasedDaysWithDayTimes
        {
            get => this.monthlyMonthDateBasedDayTimes.Keys.ToArray();
        }

        public Schedule()
        {
            this.scheduleFrequency = ScheduleFrequency.Daily;
            this.resetAllSchedules();
        }

        public Schedule(Schedule schedule) :this()
        {
            this.scheduleFrequency = schedule.ScheduleFrequency;
            this.startDate = schedule.StartDate;

            this.dailyDayFrequency = schedule.DailyDayFrequency;
            this.dailyDefaultTime = schedule.DailyDefaultTime;
            this.dailyHasAdvancedSchedule = schedule.DailyHasAdvancedSchedule;
            this.dailyDays = schedule.DailyGetCopyOfDays(schedule);
            this.dailyDayTimes = schedule.DailyGetCopyOfDayTimes(schedule);

            this.weeklyWeekFrequency = schedule.WeeklyWeekFrequency;
            this.weeklyDefaultTime = schedule.WeeklyDefaultTime;
            this.weeklyDays = schedule.WeeklyGetCopyOfWeekDays(schedule);
            this.weeklyHasAdvancedSchedule = schedule.WeeklyHasAdvancedSchedule;
            this.weeklyDayTimes = schedule.WeeklyGetCopyOfDayTimes(schedule);

            this.monthlyMonthFrequency = schedule.MonthlyMonthFrequency;
            this.monthlyDefaultTime = schedule.MonthlyDefaultTime;
            this.monthlyMonthDateBased = schedule.MonthlyDateBased;
            this.monthlyMonthDateBasedDates = schedule.monthlyMonthDateBasedGetCopyOfDates();
            this.monthlyMonthRelativeBasedCombinations = new List<MonthRelativeCombination>(schedule.MonthlyMonthRelativeBasedCombinations.Select(
                combination => new MonthRelativeCombination(combination.DayPosition, combination.DayOfWeek)));

            this.monthlyHasAdvancedSchedule = schedule.MonthlyHasAdvancedSchedule;
            this.monthlyMonthDateBasedDayTimes = schedule.MonthlyMonthDateBasedGetCopyOfDayTimes();

            this.monthlyMonthRelativeBasedDayTimes = schedule.MonthlyMonthRelativeBasedGetCopyOfDayTimes(this.monthlyMonthRelativeBasedCombinations);
        }

        public Dictionary<int, TimeSpan?[]> DailyGetCopyOfDayTimes(Schedule schedule)
        {
            Dictionary<int, TimeSpan?[]> result = new Dictionary<int, TimeSpan?[]>();

            foreach (int dayIndex in schedule.DailyDaysWithDayTimes)
            {
                result.Add(dayIndex, new TimeSpan?[3]);

                for (int index = 0; index < 3; index++)
                {
                    result[dayIndex][index] = schedule.DailyGetDayTime(dayIndex, index);
                }
            }

            return result;
        }

        public bool[] DailyGetCopyOfDays(Schedule schedule)
        {
            bool[] daysActive = new bool[3];

            for (int index = 0; index < 3; index++)
            {
                daysActive[index] = schedule.DailyGetDayActive(index);
            }

            return daysActive;
        }

        public bool[] WeeklyGetCopyOfWeekDays(Schedule schedule)
        {
            bool[] daysActive = new bool[7];

            for (int index = 0; index <= 6; index++)
            {
                daysActive[index] = schedule.WeeklyGetWeekDayActive(index);
            }

            return daysActive;
        }

        private Dictionary<DayOfWeek, TimeSpan?[]> WeeklyGetCopyOfDayTimes(Schedule schedule)
        {
            Dictionary<DayOfWeek, TimeSpan?[]> result = new Dictionary<DayOfWeek, TimeSpan?[]>();

            foreach (DayOfWeek dayOfWeek in schedule.WeeklyDaysWithDayTimes)
            {
                TimeSpan?[] times = new TimeSpan?[3];
                for (int slot = 0; slot < 3; slot++)
                {
                    times[slot] = schedule.WeeklyGetDayTime(dayOfWeek, slot);
                }

                result.Add(dayOfWeek, times);
            }

            return result;
        }

        private bool[] monthlyMonthDateBasedGetCopyOfDates()
        {
            bool[] days = new bool[31];

            for (int dayIndex = 0; dayIndex <= 30; dayIndex++)
            {
                days[dayIndex] = this.monthlyMonthDateBasedDates[dayIndex];
            }

            return days;
        }

        private Dictionary<MonthRelativeCombination, TimeSpan?[]> MonthlyMonthRelativeBasedGetCopyOfDayTimes(List<MonthRelativeCombination> existingMonthRelativeCombinations)
        {
            Dictionary<MonthRelativeCombination, TimeSpan?[]> result = new Dictionary<MonthRelativeCombination, TimeSpan?[]>();

            foreach (KeyValuePair<MonthRelativeCombination, TimeSpan?[]> dayTimes in this.monthlyMonthRelativeBasedDayTimes)
            {
                MonthRelativeCombination[] combinations =
                    existingMonthRelativeCombinations.Where(key =>
                        key.DayPosition == dayTimes.Key.DayPosition &&
                        key.DayOfWeek == dayTimes.Key.DayOfWeek).ToArray();

                if (combinations.Length < 0 || combinations.Length > 1)
                {
                    throw new ArgumentOutOfRangeException("Unexpected MonthRelativeCombination count.");
                }

                MonthRelativeCombination monthRelativeCombination;
                if (combinations.Length > 0)
                {
                    monthRelativeCombination = combinations[0];

                }
                else
                {
                    monthRelativeCombination = new MonthRelativeCombination(dayTimes.Key.DayPosition, dayTimes.Key.DayOfWeek);
                }

                result.Add(monthRelativeCombination, new TimeSpan?[3]);

                for (int index = 0; index < 3; index++)
                {
                    result[monthRelativeCombination][index] = dayTimes.Value[index];
                }
            }

            return result;
        }

        private Dictionary<int, TimeSpan?[]> MonthlyMonthDateBasedGetCopyOfDayTimes()
        {
            Dictionary<int, TimeSpan?[]> result = new Dictionary<int, TimeSpan?[]>();

            foreach (KeyValuePair<int, TimeSpan?[]> dayTimes in this.monthlyMonthDateBasedDayTimes)
            {
                result.Add(dayTimes.Key, new TimeSpan?[3]);

                for (int index = 0; index < 3; index++)
                {
                    result[dayTimes.Key][index] = dayTimes.Value[index];
                }
            }

            return result;
        }

        public void Reset()
        {
            this.scheduleFrequency = ScheduleFrequency.Daily;
            this.resetAllSchedules();
        }

        private void resetAllSchedules()
        {
            this.resetDailySchedule();
            this.resetWeeklySchedule();
            this.resetMonthlySchedule();
        }

        private void resetDailySchedule()
        {
            this.startDate = DateTime.Now;
            this.dailyDayFrequency = 1;
            this.dailyDays = new bool[3];
            this.dailyDays[0] = true;
            this.dailyHasAdvancedSchedule = false;
            this.dailyDefaultTime = new TimeSpan(0, 0, 0);
            this.dailyDayTimes = new Dictionary<int, TimeSpan?[]>();
            this.dailyDayTimes.Add(0, new TimeSpan?[3]);
            this.dailyDayTimes[0][0] = new TimeSpan();
        }

        private void resetWeeklySchedule()
        {
            this.startDate = DateTime.Now;
            this.weeklyWeekFrequency = 1;
            this.weeklyDays = new bool[7];
            this.weeklyDays[0] = true;
            this.weeklyHasAdvancedSchedule = false;
            this.weeklyDefaultTime = new TimeSpan(0, 0, 0);
            this.weeklyDayTimes = new Dictionary<DayOfWeek, TimeSpan?[]>();
            this.weeklyDayTimes.Add(DayOfWeek.Monday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Monday][0] = new TimeSpan();
        }

        private void resetMonthlySchedule()
        {
            this.startDate = DateTime.Now;
            this.monthlyMonthFrequency = 1;
            this.monthlyDefaultTime = new TimeSpan(0, 0, 0);
            this.monthlyMonthDateBased = true;
            this.monthlyHasAdvancedSchedule = false;
            this.monthlyMonthDateBasedDates = new bool[31];
            this.monthlyMonthDateBasedDates[0] = true;
            this.monthlyMonthDateBasedDayTimes = new Dictionary<int, TimeSpan?[]>();
            this.monthlyMonthDateBasedDayTimes.Add(0, new TimeSpan?[3]);
            this.monthlyMonthDateBasedDayTimes[0][0] = new TimeSpan();
            this.monthlyMonthRelativeBasedCombinations = new List<MonthRelativeCombination>();
            MonthRelativeCombination monthRelativeCombination = new MonthRelativeCombination(DayPosition.First, DayOfWeek.Monday);
            this.monthlyMonthRelativeBasedCombinations.Add(monthRelativeCombination);
            this.monthlyMonthRelativeBasedDayTimes = new Dictionary<MonthRelativeCombination, TimeSpan?[]>();
            this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombination, new TimeSpan?[3]);
            this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][0] = new TimeSpan();
        }

        public void DailySetDayTime(int dayIndex, int timeIndex, TimeSpan? time)
        {
            if (dayIndex < 0 || dayIndex > 2)
            {
                throw new InvalidOperationException("dayIndex must be between 0 and 2.");
            }

            if (timeIndex < 0 || timeIndex > 2)
            {
                throw new InvalidOperationException("timeIndex must be between 0 and 2.");
            }

            if (timeIndex == 0)
            {
                if (time == null)
                {
                    throw new InvalidScheduleException($"Day {dayIndex + 1} Time 1 must not be unset.");
                }
                else
                {
                    TimeSpan?[] timeSpans;
                    if (this.dailyDayTimes.TryGetValue(dayIndex, out timeSpans))
                    {
                        if (timeSpans[1] != null && time >= timeSpans[1])
                        {
                            throw new InvalidScheduleException($"Day {dayIndex + 1} Time 1 must be smaller than Day {dayIndex + 1} Time 2.");
                        }
                    }
                    else
                    {
                        timeSpans = new TimeSpan?[3];
                        this.dailyDayTimes.Add(dayIndex, timeSpans);
                    }
                }
            }

            if (timeIndex == 1)
            {
                if (time == null)
                {
                    if (this.dailyDayTimes[dayIndex][timeIndex + 1] != null)
                    {
                        throw new InvalidScheduleException($"Day {dayIndex + 1} Time 2 can't be unset if Day {dayIndex + 1} Time 3 has a value.");
                    }
                }
                else
                {
                    if (time <= this.dailyDayTimes[dayIndex][timeIndex - 1] ||
                        this.dailyDayTimes[dayIndex][timeIndex + 1] != null && time >= this.dailyDayTimes[dayIndex][timeIndex + 1])
                    {
                        throw new InvalidScheduleException($"Day {dayIndex + 1} Time 2 must be greater than Day {dayIndex + 1} Time 1 and smaller than Day {dayIndex + 1} Time 3.");
                    }
                }
            }

            if (timeIndex == 2)
            {
                if (time != null)
                {
                    if (this.dailyDayTimes[dayIndex][timeIndex - 1] == null)
                    {
                        throw new InvalidScheduleException($"Day {dayIndex + 1} Time 3 can't be set if Day {dayIndex + 1} Time 2 has no value.");
                    }

                    if (time <= this.dailyDayTimes[dayIndex][timeIndex - 1])
                    {
                        throw new InvalidScheduleException($"Day {dayIndex + 1} Time 3 must be greater than Day {dayIndex + 1} Time 2.");
                    }
                }
            }

            this.dailyDayTimes[dayIndex][timeIndex] = time;
        }

        public void WeeklySetDayTime(DayOfWeek dayOfWeek, int timeIndex, TimeSpan? time)
        {
            if (timeIndex == 0)
            {
                if (time == null)
                {
                    throw new InvalidScheduleException("Weekly Time 1 must not be unset.");
                }
                else
                {
                    TimeSpan?[] timeSpans;
                    if (this.weeklyDayTimes.TryGetValue(dayOfWeek, out timeSpans))
                    {
                        if (timeSpans[1] != null && time >= timeSpans[1])
                        {
                            throw new InvalidScheduleException($"{dayOfWeek} Time 1 must be smaller than {dayOfWeek} Time 2.");
                        }
                    }
                    else
                    {
                        timeSpans = new TimeSpan?[3];
                        this.weeklyDayTimes.Add(dayOfWeek, timeSpans);
                    }
                }

                this.weeklyDayTimes[dayOfWeek][0] = time;
            }

            if (timeIndex == 1)
            {
                if (time == null)
                {
                    if (this.weeklyDayTimes[dayOfWeek][timeIndex + 1] != null)
                    {
                        throw new InvalidScheduleException($"{dayOfWeek} Time 2 can't be unset if {dayOfWeek} Time 3 has a value.");
                    }
                }
                else
                {
                    if (time <= this.weeklyDayTimes[dayOfWeek][timeIndex - 1] ||
                        this.weeklyDayTimes[dayOfWeek][timeIndex + 1] != null && time >= this.weeklyDayTimes[dayOfWeek][timeIndex + 1])
                    {
                        throw new InvalidScheduleException($"{dayOfWeek} Time 2 must be greater than {dayOfWeek} Time 1 and smaller than {dayOfWeek} Time 3.");
                    }
                }
            }

            if (timeIndex == 2)
            {
                if (time != null)
                {
                    if (this.weeklyDayTimes[dayOfWeek][timeIndex - 1] == null)
                    {
                        throw new InvalidScheduleException($"{dayOfWeek} Time 3 can't be set if {dayOfWeek} Time 2 has no value.");
                    }

                    if (time <= this.weeklyDayTimes[dayOfWeek][timeIndex - 1])
                    {
                        throw new InvalidScheduleException($"{dayOfWeek} Time 3 must be greater than {dayOfWeek} Time 2.");
                    }
                }
            }

            this.weeklyDayTimes[dayOfWeek][timeIndex] = time;
        }

        public void MonthlyMonthDateBasedSetDayTime(int dayIndex, int timeIndex, TimeSpan? time)
        {
            if (timeIndex == 0)
            {
                if (time == null)
                {
                    throw new InvalidScheduleException("Monthly Time 1 must not be unset.");
                }
                else
                {
                    TimeSpan?[] timeSpans;
                    if (this.monthlyMonthDateBasedDayTimes.TryGetValue(dayIndex, out timeSpans))
                    {
                        if (timeSpans[1] != null && time >= timeSpans[1])
                        {
                            throw new InvalidScheduleException("Monthly Time 1 must be smaller than monthly Time 2.");
                        }
                    }
                    else
                    {
                        timeSpans = new TimeSpan?[3];
                        this.monthlyMonthDateBasedDayTimes.Add(dayIndex, timeSpans);
                    }
                }

                this.monthlyMonthDateBasedDayTimes[dayIndex][0] = time;
            }

            if (timeIndex == 1)
            {
                if (time == null)
                {
                    if (this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex + 1] != null)
                    {
                        throw new InvalidScheduleException("Monthly Time 2 can't be unset if Monthly Time 3 has a value.");
                    }
                }
                else
                {
                    if (time <= this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex - 1] ||
                        this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex + 1] != null && time >= this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex + 1])
                    {
                        throw new InvalidScheduleException("Monthly Time 2 must be greater than monthly Time 1 and smaller than monthly Time 3.");
                    }
                }
            }

            if (timeIndex == 2)
            {
                if (time != null)
                {
                    if (this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex - 1] == null)
                    {
                        throw new InvalidScheduleException("Monthly Time 3 can't be set if Monthly Time 2 has no value.");
                    }

                    if (time <= this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex - 1])
                    {
                        throw new InvalidScheduleException("Monthly Time 3 must be greater than Monthly Time 2.");
                    }
                }
            }

            this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex] = time;
        }

        public void MonthlyMonthRelativeBasedSetDayTime(DayPosition dayPosition, DayOfWeek dayOfWeek, int timeIndex, TimeSpan? time)
        {
            MonthRelativeCombination monthRelativeCombination = this.monthlyMonthRelativeBasedGetKeyFromDayTimes(dayPosition, dayOfWeek);

            if (timeIndex == 0)
            {
                if (time == null)
                {
                    throw new InvalidScheduleException("Monthly Time 1 must not be unset.");
                }
                else
                {
                    TimeSpan?[] timeSpans;
                    if (this.monthlyMonthRelativeBasedDayTimes.TryGetValue(monthRelativeCombination, out timeSpans))
                    {
                        if (timeSpans[1] != null && time >= timeSpans[1])
                        {
                            throw new InvalidScheduleException("Monthly Time 1 must be smaller than monthly Time 2.");
                        }
                    }
                    else
                    {
                        timeSpans = new TimeSpan?[3];
                        this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombination, timeSpans);
                    }
                }

                this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][0] = time;
            }

            if (timeIndex == 1)
            {
                if (time == null)
                {
                    if (this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][timeIndex + 1] != null)
                    {
                        throw new InvalidScheduleException("Monthly Time 2 can't be unset if Monthly Time 3 has a value.");
                    }
                }
                else
                {
                    if (time <= this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][timeIndex - 1] ||
                        this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][timeIndex + 1] != null && time >= this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][timeIndex + 1])
                    {
                        throw new InvalidScheduleException("Monthly Time 2 must be greater than monthly Time 1 and smaller than monthly Time 3.");
                    }
                }
            }

            if (timeIndex == 2)
            {
                if (time != null)
                {
                    if (this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][timeIndex - 1] == null)
                    {
                        throw new InvalidScheduleException("Monthly Time 3 can't be set if Monthly Time 2 has no value.");
                    }

                    if (time <= this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][timeIndex - 1])
                    {
                        throw new InvalidScheduleException("Monthly Time 3 must be greater than Monthly Time 2.");
                    }
                }
            }

            this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][timeIndex] = time;
        }

        public void DailySetDayActive(int dayIndex, bool active)
        {
            if (dayIndex < 1 || dayIndex > 2)
            {
                throw new InvalidOperationException("dayIndex must be between 1 and 2.");
            }

            if (active)
            {
                if (dayIndex == 2 && !this.dailyDays[1])
                {
                    throw new InvalidScheduleException("Day 3 can't be activated if day 2 isn't active.");
                }

            }

            if (!active)
            {
                if (dayIndex == 1 && this.dailyDays[2])
                {
                    throw new InvalidScheduleException("Day 2 can't be deactivated if day 3 is active.");
                }
            }

            if (active)
            {
                if (!this.dailyDayTimes.ContainsKey(dayIndex))
                {
                    this.dailyDayTimes.Add(dayIndex, new TimeSpan?[3]);
                    this.dailyDayTimes[dayIndex][0] = new TimeSpan();
                }
            }

            this.dailyDays[dayIndex] = active;
        }

        public void WeeklySetDayActive(int dayIndex, bool active)
        {
            if (dayIndex < 0 || dayIndex > 6)
            {
                throw new InvalidOperationException("dayIndex must be between 0 and 6.");
            }

            if (!active)
            {
                int count = this.weeklyDays.Count(dayActive => dayActive == true);
                if (count < 2)
                {
                    throw new InvalidScheduleException($"{Schedule.GetDayOfWeekFromDayIndex(dayIndex)} is the last active day and can't be disabled.");
                }
            }

            if (this.weeklyHasAdvancedSchedule)
            {
                DayOfWeek dayOfWeek = Schedule.GetDayOfWeekFromDayIndex(dayIndex);
                if (!this.weeklyDayTimes.ContainsKey(dayOfWeek))
                {
                    this.weeklyDayTimes.Add(dayOfWeek, new TimeSpan?[3]);
                    this.weeklyDayTimes[dayOfWeek][0] = new TimeSpan();
                }
            }

            this.weeklyDays[dayIndex] = active;
        }

        private void weeklyCheckDayTimes()
        {
            for (int dayIndex = 0; dayIndex <= 6; dayIndex++)
            {
                if (this.WeeklyGetWeekDayActive(dayIndex))
                {
                    DayOfWeek dayOfWeek = Schedule.GetDayOfWeekFromDayIndex(dayIndex);
                    if (!this.weeklyDayTimes.ContainsKey(dayOfWeek))
                    {
                        this.weeklyDayTimes.Add(dayOfWeek, new TimeSpan?[3]);
                        this.weeklyDayTimes[dayOfWeek][0] = new TimeSpan();
                    }
                }
            }
        }

        private void monthlyMonthRelativeBasedCheckDayTimes()
        {
            foreach (MonthRelativeCombination monthlyMonthRelativeBasedCombination in this.monthlyMonthRelativeBasedCombinations)
            {
                if (!this.monthlyMonthRelativeBasedDayTimes.ContainsKey(monthlyMonthRelativeBasedCombination))
                {
                    this.monthlyMonthRelativeBasedDayTimes.Add(monthlyMonthRelativeBasedCombination, new TimeSpan?[3]);
                    this.monthlyMonthRelativeBasedDayTimes[monthlyMonthRelativeBasedCombination][0] = new TimeSpan();
                }
            }
        }

        private void monthlyMonthDateBasedCheckDayTimes()
        {
            for (int dayIndex = 0; dayIndex <= 30; dayIndex++)
            {
                if (this.MonthlyMonthDateBasedGetDayActive(dayIndex))
                {
                    if (!this.monthlyMonthDateBasedDayTimes.ContainsKey(dayIndex))
                    {
                        this.monthlyMonthDateBasedDayTimes.Add(dayIndex, new TimeSpan?[3]);
                        this.monthlyMonthDateBasedDayTimes[dayIndex][0] = new TimeSpan();
                    }
                }
            }
        }

        public void MonthlyMonthDateBasedSetDayActive(int dayIndex, bool active)
        {
            if (!active)
            {
                if (this.monthlyMonthDateBasedDates.Count(dayActive => dayActive == true) < 2)
                {
                    throw new InvalidScheduleException($"{dayIndex + 1}. is the last active day and can't be disabled.");
                }
            }
            else
            {
                if (this.monthlyHasAdvancedSchedule)
                {
                    if (!this.monthlyMonthDateBasedDayTimes.ContainsKey(dayIndex))
                    {
                        this.monthlyMonthDateBasedDayTimes.Add(dayIndex, new TimeSpan?[3]);
                        this.monthlyMonthDateBasedDayTimes[dayIndex][0] = new TimeSpan();
                    }
                }
            }

            this.monthlyMonthDateBasedDates[dayIndex] = active;
        }

        public void MonthlyMonthRelativeBasedSetDayActive(DayPosition dayPosition, DayOfWeek dayOfWeek, bool active)
        {
            if (!active)
            {
                if (this.monthlyMonthRelativeBasedCombinations.Count < 2)
                {
                    throw new InvalidScheduleException("Cannot remove last relative day position.");
                }
            }

            this.monthlyMonthRelativeBasedSetDayActiveInternal(dayPosition, dayOfWeek, active);
            
            if (active)
            {
                if (this.monthlyHasAdvancedSchedule)
                {
                    MonthRelativeCombination monthRelativeCombination =
                        this.monthlyMonthRelativeBasedGetCombination(dayPosition, dayOfWeek);
                    if (!this.monthlyMonthRelativeBasedDayTimes.ContainsKey(monthRelativeCombination))
                    {
                        this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombination, new TimeSpan?[3]);
                        this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][0] = new TimeSpan();
                    }
                }
            }
        }

        private void monthlyMonthRelativeBasedSetDayActiveInternal(DayPosition dayPosition, DayOfWeek dayOfWeek, bool value)
        {
            MonthRelativeCombination[] combinations =
                this.monthlyMonthRelativeBasedCombinations.Where(key =>
                    key.DayPosition == dayPosition &&
                    key.DayOfWeek == dayOfWeek).ToArray();

            int count = combinations.Length;
            if (count > 1)
            {
                throw new ArgumentOutOfRangeException("Unexpected MonthRelativeCombination count.");
            }

            if (value)
            {
                if (count < 1)
                {
                    MonthRelativeCombination monthRelativeCombination = this.monthlyMonthRelativeBasedGetKeyFromDayTimes(dayPosition, dayOfWeek);
                    if (monthRelativeCombination == null)
                    {
                        monthRelativeCombination = new MonthRelativeCombination(dayPosition, dayOfWeek);
                    }

                    this.monthlyMonthRelativeBasedCombinations.Add(monthRelativeCombination);
                }
            }
            else
            {
                if (count > 0)
                {
                    this.monthlyMonthRelativeBasedCombinations.Remove(combinations[0]);
                }
            }
        }

        private MonthRelativeCombination monthlyMonthRelativeBasedGetKeyFromDayTimes(DayPosition dayPosition, DayOfWeek day)
        {
            MonthRelativeCombination[] combinations =
                this.monthlyMonthRelativeBasedDayTimes.Keys.Where(key =>
                    key.DayPosition == dayPosition &&
                    key.DayOfWeek == day).ToArray();

            int count = combinations.Length;
            if (count > 1)
            {
                throw new ArgumentOutOfRangeException("Unexpected MonthRelativeCombination count.");
            }

            if (count <= 0)
            {
                return null;
            }

            return combinations[0];
        }

        private MonthRelativeCombination monthlyMonthRelativeBasedGetCombination(DayPosition dayPosition, DayOfWeek dayOfWeek)
        {
            MonthRelativeCombination[] combinations =
                this.monthlyMonthRelativeBasedCombinations.Where(key =>
                    key.DayPosition == dayPosition &&
                    key.DayOfWeek == dayOfWeek).ToArray();

            int count = combinations.Length;
            if (count != 1)
            {
                throw new ArgumentOutOfRangeException("Unexpected MonthRelativeCombination count.");
            }

            return combinations[0];
        }

        public TimeSpan? DailyGetDayTime(int dayIndex, int timeIndex)
        {
            if (this.dailyDayTimes.ContainsKey(dayIndex))
            {
                return this.dailyDayTimes[dayIndex][timeIndex];
            }

            return null;
        }

        public TimeSpan? WeeklyGetDayTime(DayOfWeek dayOfWeek, int timeIndex)
        {
            return this.weeklyDayTimes[dayOfWeek][timeIndex];
        }

        public TimeSpan? MonthlyMonthDateBasedGetDayTime(int dayIndex, int timeIndex)
        {
            if (!this.monthlyMonthDateBasedDayTimes.ContainsKey(dayIndex))
            {
                return null;
            }

            return this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex];
        }

        public TimeSpan? MonthlyMonthRelativeBasedGetDayTime(DayPosition dayPosition, DayOfWeek dayOfWeek, int index)
        {
            MonthRelativeCombination monthRelativeCombination = this.monthlyMonthRelativeBasedGetKeyFromDayTimes(dayPosition, dayOfWeek);
            if (monthRelativeCombination == null)
            {
                return null;
            }
            else
            {
                return this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][index];
            }
        }

        public bool DailyGetDayActive(int dayIndex)
        {
            return this.dailyDays[dayIndex];
        }

        public bool WeeklyGetWeekDayActive(int dayIndex)
        {
            return this.weeklyDays[dayIndex];
        }

        public bool MonthlyMonthDateBasedGetDayActive(int dayIndex)
        {
            return this.monthlyMonthDateBasedDates[dayIndex];
        }

        //return next DateTime according to schedule after dateTimeAfter
        public DateTime GetNextDateTime(DateTime dateTimeAfter)
        {
            switch (this.scheduleFrequency)
            {
                case ScheduleFrequency.Daily:
                    return this.dailyGetNextDateTime(dateTimeAfter);
                    break;
                case ScheduleFrequency.Weekly:
                    return this.weeklyGetNextDateTime(dateTimeAfter);
                    break;
                case ScheduleFrequency.Monthly:
                    if (this.monthlyMonthDateBased)
                    {
                        return this.monthlyGetNextDateTimeMonthDateBased(dateTimeAfter);
                    }
                    else
                    {
                        return this.monthlyGetNextDateTimeMonthRelativeBased(dateTimeAfter);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unexpected schedule frequency.");
                    break;
            }
        }

        private DateTime dailyGetNextDateTime(DateTime dateTimeAfter)
        {
            dateTimeAfter = this.getDateTimeAfter(dateTimeAfter);
            DateTime potentialNextDate;

            if (this.dailyHasAdvancedSchedule)
            {
                potentialNextDate = this.dailyGetNextDate(dateTimeAfter);
                int dayIndex = this.dailyGetDayIndex(potentialNextDate);
                if (potentialNextDate.Date == dateTimeAfter.Date)
                {
                    TimeSpan timeOfDay;
                    if (this.dailyTryGetTimeSpanAfter(dayIndex, dateTimeAfter.TimeOfDay, out timeOfDay))
                    {
                        potentialNextDate = potentialNextDate.Add(timeOfDay);
                    }
                    else
                    {
                        potentialNextDate = this.dailyGetNextDate(potentialNextDate.AddDays(1));
                        dayIndex = this.dailyGetDayIndex(potentialNextDate);
                        potentialNextDate = potentialNextDate.Add(this.dailyDayTimes[dayIndex][0].Value);
                    }
                    
                }
                else
                {
                    potentialNextDate = potentialNextDate.Add(this.dailyDayTimes[dayIndex][0].Value);
                }

                return potentialNextDate;
            }
            else
            {
                potentialNextDate = this.dailyGetNextDate(dateTimeAfter);
                if (potentialNextDate.Date == dateTimeAfter.Date)
                {
                    if (this.dailyDefaultTime <= dateTimeAfter.TimeOfDay)
                    {
                        potentialNextDate = this.dailyGetNextDate(potentialNextDate.AddDays(1));
                    }
                }

                potentialNextDate = potentialNextDate.Add(this.dailyDefaultTime);
            }

            return potentialNextDate;
        }

        private DateTime weeklyGetNextDateTime(DateTime dateTimeAfter)
        {
            dateTimeAfter = this.getDateTimeAfter(dateTimeAfter);
            DateTime potentialNextDate;

            if (this.weeklyHasAdvancedSchedule)
            {
                potentialNextDate = this.weeklyGetNextDate(dateTimeAfter);
                if (potentialNextDate.Date == dateTimeAfter.Date)
                {
                    TimeSpan timeOfDay;
                    if (this.weeklyTryGetTimeSpanAfter(potentialNextDate.DayOfWeek, dateTimeAfter.TimeOfDay, out timeOfDay))
                    {
                        potentialNextDate = potentialNextDate.Add(timeOfDay);
                    }
                    else
                    {
                        potentialNextDate = this.weeklyGetNextDate(potentialNextDate.AddDays(1));
                        potentialNextDate = potentialNextDate.Add(this.weeklyDayTimes[potentialNextDate.DayOfWeek][0].Value);
                    }
                }
                else
                {
                    potentialNextDate = potentialNextDate.Add(this.weeklyDayTimes[potentialNextDate.DayOfWeek][0].Value);
                }

                return potentialNextDate;
            }
            else
            {
                potentialNextDate = this.weeklyGetNextDate(dateTimeAfter);
                if (potentialNextDate.Date == dateTimeAfter.Date)
                {
                    if (this.weeklyDefaultTime <= dateTimeAfter.TimeOfDay)
                    {
                        potentialNextDate = this.weeklyGetNextDate(potentialNextDate.AddDays(1));
                    }
                }

                potentialNextDate = potentialNextDate.Add(this.weeklyDefaultTime);
            }

            return potentialNextDate;
        }

        private DateTime monthlyGetNextDateTimeMonthDateBased(DateTime dateTimeAfter)
        {
            dateTimeAfter = this.getDateTimeAfter(dateTimeAfter);
            DateTime potentialNextDate;
            int potentialNextDateOriginalDay;

            if (this.monthlyHasAdvancedSchedule)
            {
                int day;
                bool corrected;

                corrected = this.monthlyGetNextDateMonthDateBased(dateTimeAfter, out potentialNextDate, out potentialNextDateOriginalDay);
                if (potentialNextDate.Date == dateTimeAfter.Date)
                {
                    TimeSpan timeOfDay;
                    day = Schedule.getDayForAdvancedSchedule(corrected, potentialNextDate, potentialNextDateOriginalDay);
                    if (this.monthlyTryGetTimeSpanAfterMonthDateBased(day, dateTimeAfter.TimeOfDay, out timeOfDay))
                    {
                        potentialNextDate = potentialNextDate.Add(timeOfDay);
                    }
                    else
                    {
                        corrected = this.monthlyGetNextDateMonthDateBased(potentialNextDate.AddDays(1), out potentialNextDate, out potentialNextDateOriginalDay);
                        day = Schedule.getDayForAdvancedSchedule(corrected, potentialNextDate, potentialNextDateOriginalDay);
                        potentialNextDate = potentialNextDate.Add(this.monthlyMonthDateBasedDayTimes[day - 1][0].Value);
                    }
                    
                }
                else
                {
                    day = Schedule.getDayForAdvancedSchedule(corrected, potentialNextDate, potentialNextDateOriginalDay);
                    potentialNextDate = potentialNextDate.Add(this.monthlyMonthDateBasedDayTimes[day - 1][0].Value);
                }
            }
            else
            {
                this.monthlyGetNextDateMonthDateBased(dateTimeAfter, out potentialNextDate, out _);
                if (potentialNextDate.Date == dateTimeAfter.Date)
                {
                    if (this.monthlyDefaultTime <= dateTimeAfter.TimeOfDay)
                    {
                        this.monthlyGetNextDateMonthDateBased(potentialNextDate.AddDays(1), out potentialNextDate, out _);
                    }
                }

                potentialNextDate = potentialNextDate.Add(this.monthlyDefaultTime);
            }

            return potentialNextDate;
        }

        private static int getDayForAdvancedSchedule(bool corrected, DateTime potentialNextDate, int potentialNextDateOriginalDay)
        {
            return corrected ? potentialNextDateOriginalDay : potentialNextDate.Day;
        }

        private DateTime monthlyGetNextDateTimeMonthRelativeBased(DateTime dateTimeAfter)
        {
            dateTimeAfter = this.getDateTimeAfter(dateTimeAfter);
            KeyValuePair<MonthRelativeCombination, DateTime> potentialNextDate;

            if (this.monthlyHasAdvancedSchedule)
            {
                potentialNextDate = this.monthlyGetNextDateMonthRelativeBased(dateTimeAfter);
                if (potentialNextDate.Value.Date == dateTimeAfter.Date)
                {
                    TimeSpan timeOfDay;
                    if (this.monthlyTryGetTimeSpanAfterMonthRelativeBased(potentialNextDate.Key, dateTimeAfter.TimeOfDay, out timeOfDay))
                    {
                        potentialNextDate = new KeyValuePair<MonthRelativeCombination, DateTime>(potentialNextDate.Key, potentialNextDate.Value.Add(timeOfDay)); 
                    }
                    else
                    {
                        potentialNextDate = this.monthlyGetNextDateMonthRelativeBased(potentialNextDate.Value.AddDays(1));
                        potentialNextDate = new KeyValuePair<MonthRelativeCombination, DateTime>(potentialNextDate.Key,
                            potentialNextDate.Value.Add(this.monthlyMonthRelativeBasedDayTimes[potentialNextDate.Key][0].Value));
                    }

                }
                else
                {
                    potentialNextDate = new KeyValuePair<MonthRelativeCombination, DateTime>(potentialNextDate.Key,
                        potentialNextDate.Value.Add(this.monthlyMonthRelativeBasedDayTimes[potentialNextDate.Key][0].Value));
                }
            }
            else
            {
                potentialNextDate = this.monthlyGetNextDateMonthRelativeBased(dateTimeAfter);
                if (potentialNextDate.Value.Date == dateTimeAfter.Date)
                {
                    if (this.monthlyDefaultTime <= dateTimeAfter.TimeOfDay)
                    {
                        potentialNextDate = this.monthlyGetNextDateMonthRelativeBased(potentialNextDate.Value.AddDays(1));
                    }
                }

                potentialNextDate = new KeyValuePair<MonthRelativeCombination, DateTime>(potentialNextDate.Key, potentialNextDate.Value.Add(this.monthlyDefaultTime));
            }

            return potentialNextDate.Value;
        }

        private bool dailyTryGetTimeSpanAfter(int dayIndex, TimeSpan timeOfDayAfter, out TimeSpan timeOfDay)
        {
            TimeSpan? timeOfDayInternal = null;
            if (this.dailyDayTimes[dayIndex][2] != null && this.dailyDayTimes[dayIndex][2].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.dailyDayTimes[dayIndex][2].Value;
            }

            if (this.dailyDayTimes[dayIndex][1] != null && this.dailyDayTimes[dayIndex][1].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.dailyDayTimes[dayIndex][1].Value;
            }

            if (this.dailyDayTimes[dayIndex][0] != null && this.dailyDayTimes[dayIndex][0].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.dailyDayTimes[dayIndex][0].Value;
            }

            if (timeOfDayInternal == null)
            {
                timeOfDay = TimeSpan.MinValue;
                return false;
            }
            else
            {
                timeOfDay = timeOfDayInternal.Value;
                return true;
            }
        }

        private bool weeklyTryGetTimeSpanAfter(DayOfWeek dayOfWeek, TimeSpan timeOfDayAfter, out TimeSpan timeOfDay)
        {
            TimeSpan? timeOfDayInternal = null;
            if (this.weeklyDayTimes[dayOfWeek][2] != null && this.weeklyDayTimes[dayOfWeek][2].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.weeklyDayTimes[dayOfWeek][2].Value;
            }

            if (this.weeklyDayTimes[dayOfWeek][1] != null && this.weeklyDayTimes[dayOfWeek][1].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.weeklyDayTimes[dayOfWeek][1].Value;
            }

            if (this.weeklyDayTimes[dayOfWeek][0] != null && this.weeklyDayTimes[dayOfWeek][0].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.weeklyDayTimes[dayOfWeek][0].Value;
            }

            if (timeOfDayInternal == null)
            {
                timeOfDay = TimeSpan.MinValue;
                return false;
            }
            else
            {
                timeOfDay = timeOfDayInternal.Value;
                return true;
            }
        }

        private bool monthlyTryGetTimeSpanAfterMonthDateBased(int day, TimeSpan timeOfDayAfter, out TimeSpan timeOfDay)
        {
            TimeSpan? timeOfDayInternal = null;
            int dayIndex = day - 1;
            if (this.monthlyMonthDateBasedDayTimes[dayIndex][2] != null && this.monthlyMonthDateBasedDayTimes[dayIndex][2].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.monthlyMonthDateBasedDayTimes[dayIndex][2].Value;
            }

            if (this.monthlyMonthDateBasedDayTimes[dayIndex][1] != null && this.monthlyMonthDateBasedDayTimes[dayIndex][1].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.monthlyMonthDateBasedDayTimes[dayIndex][1].Value;
            }

            if (this.monthlyMonthDateBasedDayTimes[dayIndex][0] != null && this.monthlyMonthDateBasedDayTimes[dayIndex][0].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.monthlyMonthDateBasedDayTimes[dayIndex][0].Value;
            }

            if (timeOfDayInternal == null)
            {
                timeOfDay = TimeSpan.MinValue;
                return false;
            }
            else
            {
                timeOfDay = timeOfDayInternal.Value;
                return true;
            }
        }

        private bool monthlyTryGetTimeSpanAfterMonthRelativeBased(MonthRelativeCombination monthRelativeCombination, TimeSpan timeOfDayAfter, out TimeSpan timeOfDay)
        {
            TimeSpan? timeOfDayInternal = null;
            if (this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][2] != null && this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][2].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][2].Value;
            }

            if (this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][1] != null && this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][1].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][1].Value;
            }

            if (this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][0] != null && this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][0].Value > timeOfDayAfter)
            {
                timeOfDayInternal = this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][0].Value;
            }

            if (timeOfDayInternal == null)
            {
                timeOfDay = TimeSpan.MinValue;
                return false;
            }
            else
            {
                timeOfDay = timeOfDayInternal.Value;
                return true;
            }
        }

        private DateTime dailyGetNextDate(DateTime dateTime)
        {
            DateTime potentialNextDate = dateTime.Date;
            int remainder = (potentialNextDate.Date - this.startDate.Date).Days % this.DailyDayFrequency;
            if (remainder > 0)
            {
                potentialNextDate = potentialNextDate.AddDays(this.DailyDayFrequency - remainder);
            }

            return potentialNextDate;
        }

        private DateTime weeklyGetNextDate(DateTime dateTime)
        {
            int weekIndex = Schedule.weeklyGetWeekIndex(this.startDate, dateTime);
            int remainder = weekIndex % this.weeklyWeekFrequency;
            if (remainder > 0)
            {
                weekIndex = weekIndex + (this.weeklyWeekFrequency - remainder);
            }

            int dayIndex = Schedule.GetCorrectedDayIndex(dateTime.DayOfWeek);

            for (int index = dayIndex; index <= 6; index++)
            {
                if (this.weeklyDays[index])
                {
                    return Schedule.weeklyGetBeginningOfWeek(this.startDate).AddDays(weekIndex * 7 + index);
                }
            }

            //current day or no day after (in this week) are enabled in this schedule, so we take first valid day of next week
            dayIndex = Array.IndexOf(this.weeklyDays, true);
            weekIndex = weekIndex + this.weeklyWeekFrequency;
            return Schedule.weeklyGetBeginningOfWeek(this.startDate).AddDays(weekIndex * 7 + dayIndex);
        }

        //slightly different signature as for daily and weekly, as we need the original date
        //if date was corrected to the last day of the month, if day doesn't exist in this mont.
        //the original date is needed later to get the time on the advance schedule
        private bool monthlyGetNextDateMonthDateBased(DateTime dateTime, out DateTime nextDate, out int originalDay)
        {
            bool corrected = false;
            int monthIndex = Schedule.monthlyGetMonthIndex(this.startDate, dateTime);
            int remainder = monthIndex % this.monthlyMonthFrequency;
            if (remainder > 0)
            {
                monthIndex = monthIndex + (this.monthlyMonthFrequency - remainder);
            }

            int originalDayIndex;
            for (int index = dateTime.Day - 1; index < 31; index++)
            {
                originalDayIndex = index;
                if (this.monthlyMonthDateBasedDates[index])
                {
                    int maxDays = DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
                    if (index + 1 > maxDays)
                    {
                        corrected = true;
                        index = maxDays - 1;
                    }

                    nextDate = Schedule.monthlyGetBeginningOfMonth(this.startDate).AddMonths(monthIndex).AddDays(index);
                    originalDay = originalDayIndex + 1;
                    return corrected;
                }
            }

            //current day or no day after (in this month) are enabled in this schedule, so we take first valid day of next month
            int dayIndex = Array.IndexOf(this.monthlyMonthDateBasedDates, true);
            monthIndex = monthIndex + this.monthlyMonthFrequency;
            nextDate = Schedule.monthlyGetBeginningOfMonth(this.startDate).AddMonths(monthIndex).AddDays(dayIndex);
            originalDay = dayIndex;
            return corrected;
        }

        private static DateTime monthlyGetBeginningOfMonth(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        private KeyValuePair<MonthRelativeCombination, DateTime> monthlyGetNextDateMonthRelativeBased(DateTime dateTime)
        {
            int monthIndex = Schedule.monthlyGetMonthIndex(this.startDate, dateTime);
            int remainder = monthIndex % this.monthlyMonthFrequency;
            if (remainder > 0)
            {
                monthIndex = monthIndex + (this.monthlyMonthFrequency - remainder);
            }

            DateTime monthStart = Schedule.monthlyGetBeginningOfMonth(this.startDate).AddMonths(monthIndex);

            Dictionary<MonthRelativeCombination, DateTime> monthDates = new Dictionary<MonthRelativeCombination, DateTime>();
            foreach (MonthRelativeCombination monthlyMonthRelativeBasedCombination in this.monthlyMonthRelativeBasedCombinations)
            {
                DateTime? date = Schedule.monthlyGetDayOfMonth(monthStart,
                    monthlyMonthRelativeBasedCombination.DayOfWeek, monthlyMonthRelativeBasedCombination.DayPosition);

                if (date != null && !monthDates.Values.Contains(date.Value))
                {
                    monthDates.Add(monthlyMonthRelativeBasedCombination, date.Value);
                }
            }

            KeyValuePair<MonthRelativeCombination, DateTime>[] orderedMonthDates = monthDates.OrderBy(entry => entry.Value).ToArray();
            if (orderedMonthDates.Any(entry => entry.Value.Date >= dateTime.Date))
            {
                return orderedMonthDates.First(entry => entry.Value.Date >= dateTime.Date);
                
            }

            return this.monthlyGetNextDateMonthRelativeBased(monthStart.AddMonths(1));
        }

        private static DateTime? monthlyGetDayOfMonth(DateTime month, DayOfWeek day, DayPosition dayPosition)
        {
            int dayPositionInt = (int)dayPosition;
            if (dayPositionInt <= 4)
            {
                month = new DateTime(month.Year, month.Month, 1);
                int firstDay = (int)month.DayOfWeek;
                int searchedDay = (int)day;
                if (searchedDay < firstDay)
                {
                    searchedDay += 7;
                }

                DateTime result = month.AddDays((searchedDay - firstDay) + 7 * dayPositionInt);
                if (result.Month == month.Month)
                {
                    return result;
                }

                return null;
            }
            else
            {
                dayPositionInt = dayPositionInt - 5;
                int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
                month = new DateTime(month.Year, month.Month, daysInMonth);
                int lastDay = ((int)month.DayOfWeek);
                int searchedDay = ((int)day);

                if (searchedDay > lastDay)
                {
                    lastDay += 7;
                }

                DateTime result = month.AddDays((searchedDay - lastDay) - 7 * dayPositionInt);
                if (result.Month == month.Month)
                {
                    return result;
                }

                return null;
            }
        }

        private DateTime getDateTimeAfter(DateTime dateTimeAfter)
        {
            //Newly schedule upload shall be at least 24 hour in the future
            DateTime dateTimeAfterResult = DateTime.Now.AddHours(24);

            if (this.startDate > dateTimeAfterResult)
            {
                dateTimeAfterResult = this.startDate;
            }

            if (this.uploadedUntil > dateTimeAfterResult)
            {
                dateTimeAfterResult = this.uploadedUntil;
            }

            if (dateTimeAfter > dateTimeAfterResult)
            {
                dateTimeAfterResult = dateTimeAfter;
            }

            return dateTimeAfterResult;
        }

        private int dailyGetDayIndex(DateTime potentialNextDate)
        {
            int dayIndexes = 1;
            if (this.dailyDays[1])
            {
                dayIndexes = 2;
            }

            if (this.dailyDays[2])
            {
                dayIndexes = 3;
            }

            int dayIndex = (potentialNextDate.Date - this.startDate.Date).Days / this.dailyDayFrequency % dayIndexes;
            return dayIndex;
        }

        private static int weeklyGetWeekIndex(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
                throw new ArgumentException("endDate cannot be less than startDate");

            return (Schedule.weeklyGetBeginningOfWeek(endDate).Subtract(Schedule.weeklyGetBeginningOfWeek(startDate)).Days / 7);
        }

        private static DateTime weeklyGetBeginningOfWeek(DateTime date)
        {
            int dayIndex = (int) date.DayOfWeek - 1;
            if (dayIndex < 0)
            {
                dayIndex = 6;
            }
            return date.AddDays(-1 * dayIndex).Date;
        }

        private static int monthlyGetMonthIndex(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
                throw new ArgumentException("endDate cannot be less than startDate");

            return ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
        }

        //corrects day index to start with monday and not with sunday
        public static int GetCorrectedDayIndex(DayOfWeek dayOfWeek)
        {
            int dayIndex = (int)dayOfWeek - 1;
            if (dayIndex < 0)
            {
                dayIndex = 6;
            }

            return dayIndex;
        }

        public static DayOfWeek GetDayOfWeekFromDayIndex(int dayIndex)
        {
            dayIndex = dayIndex + 1;
            if (dayIndex > 6)
            {
                dayIndex = 0;
            }

            return (DayOfWeek)dayIndex;
        }
    }
}
