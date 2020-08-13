using System;
using System.Collections.Generic;
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
        private DateTime dailyStartDate;
        [JsonProperty]
        private DateTime dailyUploadedUntil;
        [JsonProperty]
        private bool dailyIgnoreUploadedBeforeStartDate;
        [JsonProperty]
        private int dailyDayFrequency;
        [JsonProperty]
        private TimeSpan dailyDefaultTime;
        [JsonProperty]
        private bool dailyHasAdvancedSchedule;
        [JsonProperty]
        private Dictionary<int, TimeSpan?[]> dailyDayTimes;

        [JsonProperty]
        private DateTime weeklyStartDate;
        [JsonProperty]
        private DateTime weeklyUploadedUntil;
        [JsonProperty]
        private bool weeklyIgnoreUploadedBeforeStartDate;
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
        private DateTime monthlyStartDate;
        [JsonProperty]
        private DateTime monthlyUploadedUntil;
        [JsonProperty]
        private bool monthlyIgnoreUploadedBeforeStartDate;
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

        public DateTime DailyStartDate
        {
            get => this.dailyStartDate;
            set
            {
                this.dailyStartDate = value;
                this.dailyUploadedUntil = DateTime.MinValue;
            }
        }

        public DateTime DailyUploadedUntil
        {
            get => this.dailyUploadedUntil;
            set => this.dailyUploadedUntil = value;
        }

        public bool DailyIgnoreUploadedBeforeStartDate
        {
            get => this.dailyIgnoreUploadedBeforeStartDate;
            set => this.dailyIgnoreUploadedBeforeStartDate = value;
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

        public Dictionary<int, TimeSpan?[]> DailyDayTimes
        {
            get => this.dailyDayTimes;
            set => this.dailyDayTimes = value;
        }

        public DateTime WeeklyStartDate
        {
            get => this.weeklyStartDate;
            set
            {
                this.weeklyStartDate = value;
                this.weeklyUploadedUntil = DateTime.MinValue;
            }
        }

        public DateTime WeeklyUploadedUntil
        {
            get => this.weeklyUploadedUntil;
            set => this.weeklyUploadedUntil = value;
        }

        public bool WeeklyIgnoreUploadedBeforeStartDate
        {
            get => this.weeklyIgnoreUploadedBeforeStartDate;
            set => this.weeklyIgnoreUploadedBeforeStartDate = value;
        }

        public int WeeklyWeekFrequency
        {
            get => this.weeklyWeekFrequency;
            set => this.weeklyWeekFrequency = value;
        }

        public bool[] WeeklyDays
        {
            get => this.weeklyDays;
            set => this.weeklyDays = value;
        }

        public bool WeeklyHasAdvancedSchedule
        {
            get => this.weeklyHasAdvancedSchedule;
            set => this.weeklyHasAdvancedSchedule = value;
        }

        public TimeSpan WeeklyDefaultTime
        {
            get => this.weeklyDefaultTime;
            set => this.weeklyDefaultTime = value;
        }

        public Dictionary<DayOfWeek, TimeSpan?[]> WeeklyDayTimes
        {
            get => this.weeklyDayTimes;
            set => this.weeklyDayTimes = value;
        }

        public DateTime MonthlyStartDate
        {
            get => this.monthlyStartDate;
            set
            {
                this.monthlyStartDate = value;
                this.monthlyUploadedUntil = DateTime.MinValue;
            }
        }

        public DateTime MonthlyUploadedUntil
        {
            get => this.monthlyUploadedUntil;
            set => this.monthlyUploadedUntil = value;
        }

        public bool MonthlyIgnoreUploadedBeforeStartDate
        {
            get => this.monthlyIgnoreUploadedBeforeStartDate;
            set => this.monthlyIgnoreUploadedBeforeStartDate = value;
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
            set => this.monthlyMonthDateBased = value;
        }

        public bool MonthlyHasAdvancedSchedule
        {
            get => this.monthlyHasAdvancedSchedule;
            set => this.monthlyHasAdvancedSchedule = value;
        }

        public bool[] MonthlyMonthDateBasedDates
        {
            get => this.monthlyMonthDateBasedDates;
            set => this.monthlyMonthDateBasedDates = value;
        }

        public List<MonthRelativeCombination> MonthlyMonthRelativeBasedCombinations
        {
            get => this.monthlyMonthRelativeBasedCombinations;
            set => this.monthlyMonthRelativeBasedCombinations = value;
        }

        public Dictionary<int, TimeSpan?[]> MonthlyMonthDateBasedDayTimes
        {
            get => this.monthlyMonthDateBasedDayTimes;
            set => this.monthlyMonthDateBasedDayTimes = value;
        }

        public Dictionary<MonthRelativeCombination, TimeSpan?[]> MonthlyMonthRelativeBasedDayTimes
        {
            get => this.monthlyMonthRelativeBasedDayTimes;
            set => this.monthlyMonthRelativeBasedDayTimes = value;
        }

        public Schedule()
        {
            this.scheduleFrequency = ScheduleFrequency.Daily;
            this.resetAllSchedules();
        }

        public Schedule(Schedule schedule) :this()
        {
            this.scheduleFrequency = schedule.ScheduleFrequency;

            this.dailyStartDate = schedule.DailyStartDate;
            this.dailyIgnoreUploadedBeforeStartDate = schedule.DailyIgnoreUploadedBeforeStartDate;
            this.dailyDayFrequency = schedule.DailyDayFrequency;
            this.dailyDefaultTime = schedule.DailyDefaultTime;
            this.dailyHasAdvancedSchedule = schedule.DailyHasAdvancedSchedule;
            this.dailyDayTimes = schedule.DailyDayTimes.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());

            this.weeklyStartDate = schedule.weeklyStartDate;
            this.weeklyIgnoreUploadedBeforeStartDate = schedule.weeklyIgnoreUploadedBeforeStartDate;
            this.weeklyWeekFrequency = schedule.WeeklyWeekFrequency;
            this.weeklyDefaultTime = schedule.WeeklyDefaultTime;
            schedule.WeeklyDays.CopyTo(this.weeklyDays, 0);
            this.weeklyHasAdvancedSchedule = schedule.WeeklyHasAdvancedSchedule;
            this.weeklyDayTimes = schedule.WeeklyDayTimes.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());

            this.monthlyStartDate = schedule.monthlyStartDate;
            this.monthlyIgnoreUploadedBeforeStartDate = schedule.monthlyIgnoreUploadedBeforeStartDate;
            this.monthlyMonthFrequency = schedule.MonthlyMonthFrequency;
            this.monthlyDefaultTime = schedule.MonthlyDefaultTime;
            this.monthlyMonthDateBased = schedule.MonthlyDateBased;
            schedule.MonthlyMonthDateBasedDates.CopyTo(this.monthlyMonthDateBasedDates, 0);
            this.monthlyMonthRelativeBasedCombinations = new List<MonthRelativeCombination>(schedule.MonthlyMonthRelativeBasedCombinations.Select(
                combination => new MonthRelativeCombination(combination.DayPosition, combination.DayOfWeek)));
            this.monthlyHasAdvancedSchedule = schedule.MonthlyHasAdvancedSchedule;
            this.monthlyMonthDateBasedDayTimes = schedule.MonthlyMonthDateBasedDayTimes.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());

            this.monthlyMonthRelativeBasedDayTimes = new Dictionary<MonthRelativeCombination, TimeSpan?[]>();
            foreach (KeyValuePair<MonthRelativeCombination, TimeSpan?[]> scheduleMonthlyMonthRelativeBasedDayTime in schedule.MonthlyMonthRelativeBasedDayTimes)
            {
                MonthRelativeCombination monthRelativeCombination = this.getMonthRelativeBasedCombination(
                    scheduleMonthlyMonthRelativeBasedDayTime.Key.DayPosition, scheduleMonthlyMonthRelativeBasedDayTime.Key.DayOfWeek);

                if (monthRelativeCombination == null)
                {
                    monthRelativeCombination = new MonthRelativeCombination(scheduleMonthlyMonthRelativeBasedDayTime.Key.DayPosition, scheduleMonthlyMonthRelativeBasedDayTime.Key.DayOfWeek);
                }

                this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombination, scheduleMonthlyMonthRelativeBasedDayTime.Value.ToArray());
            }
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
            this.dailyStartDate = DateTime.Now;
            this.dailyDayFrequency = 1;
            this.dailyHasAdvancedSchedule = false;
            this.dailyDefaultTime = new TimeSpan(0, 0, 0);
            this.dailyDayTimes = new Dictionary<int, TimeSpan?[]>();
            this.dailyDayTimes.Add(0, new TimeSpan?[3]);
            this.dailyDayTimes[0][0] = new TimeSpan();
        }

        private void resetWeeklySchedule()
        {
            this.weeklyStartDate = DateTime.Now;
            this.weeklyWeekFrequency = 1;
            this.weeklyDays = new bool[7];
            this.weeklyDays[0] = true;
            this.weeklyHasAdvancedSchedule = false;
            this.weeklyDefaultTime = new TimeSpan(0, 0, 0);
            this.weeklyDayTimes = new Dictionary<DayOfWeek, TimeSpan?[]>();
            this.weeklyDayTimes.Add(DayOfWeek.Monday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Monday][0] = new TimeSpan();
            this.weeklyDayTimes.Add(DayOfWeek.Tuesday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Tuesday][0] = new TimeSpan();
            this.weeklyDayTimes.Add(DayOfWeek.Wednesday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Wednesday][0] = new TimeSpan();
            this.weeklyDayTimes.Add(DayOfWeek.Thursday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Thursday][0] = new TimeSpan();
            this.weeklyDayTimes.Add(DayOfWeek.Friday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Friday][0] = new TimeSpan();
            this.weeklyDayTimes.Add(DayOfWeek.Saturday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Saturday][0] = new TimeSpan();
            this.weeklyDayTimes.Add(DayOfWeek.Sunday, new TimeSpan?[3]);
            this.weeklyDayTimes[DayOfWeek.Sunday][0] = new TimeSpan();
        }

        private void resetMonthlySchedule()
        {
            this.monthlyStartDate = DateTime.Now;
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

        private MonthRelativeCombination getMonthRelativeBasedCombination(DayPosition dayPosition, DayOfWeek day)
        {
            MonthRelativeCombination[] combinations =
                this.monthlyMonthRelativeBasedCombinations.Where(key =>
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
                    if (this.monthlyHasAdvancedSchedule)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
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
            if (this.dailyHasAdvancedSchedule)
            {
                DateTime potentialNextDate = this.dailyGetNextDate(dateTimeAfter);
                int dayIndex = this.dailyGetDayIndex(potentialNextDate);
                if (potentialNextDate.Date > dateTimeAfter.Date)
                {
                    return potentialNextDate.Add(this.dailyDayTimes[dayIndex][0].Value);
                }
                else
                {
                    TimeSpan timeOfDay;
                    if (this.dailyTryGetTimeSpanAfter(dayIndex, dateTimeAfter.TimeOfDay, out timeOfDay))
                    {
                        return potentialNextDate.Add(timeOfDay);
                    }
                    else
                    {
                        potentialNextDate = this.dailyGetNextDate(potentialNextDate.AddDays(1));
                        dayIndex = this.dailyGetDayIndex(potentialNextDate);
                        return potentialNextDate.Add(this.dailyDayTimes[dayIndex][0].Value);
                    }
                }
            }
            else
            {
                DateTime potentialNextDate = this.dailyGetNextDate(dateTimeAfter);
                if (this.dailyDefaultTime <= dateTimeAfter.TimeOfDay)
                {
                    potentialNextDate = this.dailyGetNextDate(potentialNextDate.AddDays(1));
                }

                potentialNextDate = potentialNextDate.Add(this.dailyDefaultTime);
                return potentialNextDate;
            }
        }

        private DateTime weeklyGetNextDateTime(DateTime dateTimeAfter)
        {
            dateTimeAfter = this.getDateTimeAfter(dateTimeAfter);
            if (this.weeklyHasAdvancedSchedule)
            {
                DateTime potentialNextDate = this.weeklyGetNextDate(dateTimeAfter);
                if (potentialNextDate.Date > dateTimeAfter.Date)
                {
                    return potentialNextDate.Add(this.weeklyDayTimes[potentialNextDate.DayOfWeek][0].Value);
                }
                else
                {
                    TimeSpan timeOfDay;
                    if (this.weeklyTryGetTimeSpanAfter(potentialNextDate.DayOfWeek, dateTimeAfter.TimeOfDay, out timeOfDay))
                    {
                        return potentialNextDate.Add(timeOfDay);
                    }
                    else
                    {
                        potentialNextDate = this.weeklyGetNextDate(potentialNextDate.AddDays(1));
                        return potentialNextDate.Add(this.weeklyDayTimes[potentialNextDate.DayOfWeek][0].Value);
                    }
                }
            }
            else
            {
                DateTime potentialNextDate = this.weeklyGetNextDate(dateTimeAfter);
                if (this.weeklyDefaultTime <= dateTimeAfter.TimeOfDay)
                {
                    potentialNextDate = this.weeklyGetNextDate(potentialNextDate.AddDays(1));
                }

                potentialNextDate = potentialNextDate.Add(this.weeklyDefaultTime);
                return potentialNextDate;
            }
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

        private DateTime dailyGetNextDate(DateTime dateTime)
        {
            DateTime potentialNextDate = dateTime.Date;
            int remainder = (potentialNextDate.Date - this.dailyStartDate.Date).Days % this.DailyDayFrequency;
            if (remainder > 0)
            {
                potentialNextDate = potentialNextDate.AddDays(this.DailyDayFrequency - remainder);
            }

            return potentialNextDate;
        }

        private DateTime weeklyGetNextDate(DateTime dateTime)
        {
            int weekIndex = Schedule.weeklyGetWeekIndex(this.weeklyStartDate, dateTime);
            int remainder = weekIndex % this.weeklyWeekFrequency;
            if (remainder > 0)
            {
                weekIndex = weekIndex + (this.weeklyWeekFrequency - remainder);
            }

            int dayIndex = (int)dateTime.DayOfWeek - 1;
            if (dayIndex < 0)
            {
                dayIndex = 6;
            }

            for (int index = dayIndex; index <= 6; index++)
            {
                if (this.weeklyDays[index])
                {
                    return Schedule.weeklyGetBeginningOfWeek(this.weeklyStartDate).AddDays(weekIndex * 7 + index);
                }
            }

            //current day or no day after (in this week) are enabled in this schedule, so we take first valid day of next week
            dayIndex = Array.IndexOf(this.weeklyDays, true);
            weekIndex = weekIndex + this.weeklyWeekFrequency;
            return Schedule.weeklyGetBeginningOfWeek(this.weeklyStartDate).AddDays(weekIndex * 7 + dayIndex);
        }

        private DateTime getDateTimeAfter(DateTime plannedUntil)
        {
            //Newly schedule upload shal be at least 24 hour in the future
            DateTime dateAfter = DateTime.Now.AddHours(24);

            if (this.dailyStartDate > dateAfter)
            {
                dateAfter = this.dailyStartDate;
            }

            if (this.dailyUploadedUntil > dateAfter)
            {
                dateAfter = this.dailyUploadedUntil;
            }

            if (plannedUntil > dateAfter)
            {
                dateAfter = plannedUntil;
            }

            return dateAfter;
        }

        private int dailyGetDayIndex(DateTime potentialNextDate)
        {
            int dayIndexes = 1;
            if (this.DailyDayTimes[1] != null && this.DailyDayTimes[1][1] != null)
            {
                dayIndexes = 2;
            }

            if (this.DailyDayTimes[2] != null && this.DailyDayTimes[2][1] != null)
            {
                dayIndexes = 3;
            }

            int dayIndex = (potentialNextDate.Date - this.dailyStartDate.Date).Days / this.dailyDayFrequency % dayIndexes;
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
    }
}
