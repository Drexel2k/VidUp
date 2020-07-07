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
        private DateTime dailyStartDate;
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

        public DateTime DailyStartDate
        {
            get => this.dailyStartDate;
            set => this.dailyStartDate = value;
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
            set => this.weeklyStartDate = value;
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
            set => this.monthlyStartDate = value;
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

        public List<MonthRelativeCombination> MonthlyMonthRelativeCombinations
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
            this.monthlyMonthRelativeBasedCombinations = new List<MonthRelativeCombination>(schedule.MonthlyMonthRelativeCombinations.Select(
                combination => new MonthRelativeCombination(combination.DayPosition, combination.DayOfWeek)));
            this.monthlyHasAdvancedSchedule = schedule.MonthlyHasAdvancedSchedule;
            this.monthlyMonthDateBasedDayTimes = schedule.MonthlyMonthDateBasedDayTimes.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());
            this.monthlyMonthRelativeBasedDayTimes = schedule.MonthlyMonthRelativeBasedDayTimes.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());
        }

        private void resetAllSchedules()
        {
            this.resetDailySchedule();
            this.resetWeeklySchedule();
            this.resetMonthlySchedule();
        }

        private void resetDailySchedule()
        {
            this.dailyDayFrequency = 1;
            this.dailyHasAdvancedSchedule = false;
            this.dailyDefaultTime = new TimeSpan(0, 0, 0);
            this.dailyDayTimes = new Dictionary<int, TimeSpan?[]>();
            this.dailyDayTimes.Add(0, new TimeSpan?[3]);
            this.dailyDayTimes[0][0] = new TimeSpan();
        }

        private void resetWeeklySchedule()
        {
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
    }
}
