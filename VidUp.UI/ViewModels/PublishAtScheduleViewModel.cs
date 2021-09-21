using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    public class PublishAtScheduleViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand resetCommand;

        private bool validate = false;

        private Schedule schedule;
        private QuarterHourViewModels quarterHourViewModelsEmptyStartValue;
        private QuarterHourViewModels quarterHourViewModels;

        private DateTime specificDateDateTime;

        private bool[] dailyDays;
        private Dictionary<int, TimeSpan?[]> dailyDayTimes;

        private bool[] weeklyDays;
        private Dictionary<DayOfWeek, TimeSpan?[]> weeklyDayTimes;

        private bool[] monthlyMonthDateBasedDates;

        private List<MonthDayViewModel> monthlyMonthDateBasedDayViewModels;
        private MonthDayViewModel monthlyMonthDateBasedDay;
        private Dictionary<int, TimeSpan?[]> monthlyMonthDateBasedDayTimes;

        private ObservableCollection<MonthRelativeCombinationViewModel> monthlyMonthRelativeBasedCombinationViewModels;
        private MonthRelativeCombinationViewModel monthlyMonthRelativeBasedCombination;
        private DayOfWeek monthlyMonthRelativeBasedDay;
        private DayPosition monthlyMonthRelativeBasedDayPosition;
        private List<MonthRelativeCombination> monthlyMonthRelativeBasedCombinations;
        private Dictionary<MonthRelativeCombination, TimeSpan?[]> monthlyMonthRelativeBasedDayTimes;

        public GenericCommand ResetCommand
        {
            get
            {
                return this.resetCommand;
            }
        }

        public bool Validate
        {
            get => this.validate;
            set
            {
                this.validate = value;
            }
        }

        public Schedule Schedule
        {
            get => this.schedule;
        }

        public Array ScheduleFrequencies
        {
            get { return Enum.GetValues(typeof(ScheduleFrequency)); }
        }

        public ScheduleFrequency ScheduleFrequency
        {
            get => this.schedule.ScheduleFrequency;
            set
            {
                this.schedule.ScheduleFrequency = value;
                this.raisePropertyChanged("ScheduleFrequency");
            }
        }

        public QuarterHourViewModels QuarterHourViewModelsEmptyStartValue
        {
            get
            {
                return this.quarterHourViewModelsEmptyStartValue;
            }
        }

        public QuarterHourViewModels QuarterHourViewModels
        {
            get
            {
                return this.quarterHourViewModels;
            }
        }

        #region specific
        public DateTime SpecificDateFirstDate
        {
            get => DateTime.Now.AddDays(1).Date;
        }

        public DateTime SpecificDateDate
        {
            get => this.specificDateDateTime.Date;
            set
            {
                this.specificDateDateTime = value.Add(this.specificDateDateTime.TimeOfDay);
            }
        }

        public QuarterHourViewModel SpecificDateTime
        { 
            get =>  this.quarterHourViewModels.GetQuarterHourViewModel(this.specificDateDateTime.TimeOfDay);
            set
            {
                this.specificDateDateTime = this.specificDateDateTime.Date.Add(value.QuarterHour.Value);
            }
        }

        #endregion

        #region daily
        public int DailyDayFrequency
        {
            get => this.schedule.DailyDayFrequency;
            set
            {
                this.schedule.DailyDayFrequency = value;
                this.raisePropertyChanged("DailyDayFrequency");
            }
        }
        public QuarterHourViewModel DailyDefaultTime
        {
            get
            {
                return this.quarterHourViewModels.GetQuarterHourViewModel(this.schedule.DailyDefaultTime);
            }
            set
            {
                this.schedule.DailyDefaultTime = value.QuarterHour.Value;
                this.raisePropertyChanged("DailyDefaultTime");
            }
        }

        public bool DailyDefaultTimeEnabled
        {
            get
            {
                return !this.schedule.DailyHasAdvancedSchedule;
            }
        }

        public bool DailyHasAdvancedSchedule
        {
            get
            {
                return this.schedule.DailyHasAdvancedSchedule;
            }

            set
            {
                this.schedule.DailyHasAdvancedSchedule = value;
                this.raisePropertyChanged("DailyHasAdvancedSchedule");
                this.dailyAdjustControlEnablement();
            }
        }

        public bool DailyDay2Active
        {
            get => this.dailyDays[1];
            set => this.dailyDays[1] = value;
        }

        public bool DailyDay3Active
        {
            get => this.dailyDays[2];
            set => this.dailyDays[2] = value;
        }

        public bool DailyDay2ActiveEnabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay3ActiveEnabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay1Time1Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(0))
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay1Time2Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(0))
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay1Time3Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(0))
                {
                    return false;
                }

                if (this.schedule.DailyGetDayTime(0, 1) != null)
                {
                    return true;
                }

                return false;
            }
        }

        public bool DailyDay2Time1Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(1))
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay2Time2Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(1))
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay2Time3Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(1))
                {
                    return false;
                }

                if (this.schedule.DailyGetDayTime(1, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay3Time1Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(2))
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay3Time2Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(2))
                {
                    return false;
                }

                return true;
            }
        }

        public bool DailyDay3Time3Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.DailyGetDayActive(2))
                {
                    return false;
                }

                if (this.schedule.DailyGetDayTime(2, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public QuarterHourViewModel DailyDay1Time1
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(0))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.dailyDayTimes[0][0]);
            }
            set
            {
                this.dailyDayTimes[0][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay1Time2
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(0))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.dailyDayTimes[0][1]);
            }
            set
            {
                this.dailyDayTimes[0][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay1Time3
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(0))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.dailyDayTimes[0][2]);
            }
            set
            {
                this.dailyDayTimes[0][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay2Time1
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(1))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.dailyDayTimes[1][0]);
            }
            set
            {
                this.dailyDayTimes[1][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay2Time2
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(1))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.dailyDayTimes[1][1]);
            }
            set
            {
                this.dailyDayTimes[1][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay2Time3
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(1))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.dailyDayTimes[1][2]);
            }
            set
            {
                this.dailyDayTimes[1][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay3Time1
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(2))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.dailyDayTimes[2][0]);
            }
            set
            {
                this.dailyDayTimes[2][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay3Time2
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(2))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.dailyDayTimes[2][1]);
            }
            set
            {
                this.dailyDayTimes[2][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel DailyDay3Time3
        {
            get
            {
                if (!this.dailyDayTimes.ContainsKey(2))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.dailyDayTimes[2][2]);
            }
            set
            {
                this.dailyDayTimes[2][2] = value.QuarterHour;
            }
        }

        #endregion

        #region weekly
        public int WeeklyWeekFrequency
        {
            get => this.schedule.WeeklyWeekFrequency;
            set
            {
                this.schedule.WeeklyWeekFrequency = value;
                this.raisePropertyChanged("WeeklyWeekFrequency");
            }
        }
        public QuarterHourViewModel WeeklyDefaultTime
        {
            get
            {
                return this.quarterHourViewModels.GetQuarterHourViewModel(this.schedule.WeeklyDefaultTime);
            }
            set
            {
                this.schedule.WeeklyDefaultTime = value.QuarterHour.Value;
                this.raisePropertyChanged("WeeklyDefaultTime");
            }
        }

        public bool WeeklyDefaultTimeEnabled
        {
            get
            {
                return !this.schedule.WeeklyHasAdvancedSchedule;
            }
        }

        public bool WeeklyHasAdvancedSchedule
        {
            get
            {
                return this.schedule.WeeklyHasAdvancedSchedule;
            }

            set
            {
                this.schedule.WeeklyHasAdvancedSchedule = value;

                //if advanced day times didn't contain activated days yet.
                if (value)
                {
                    this.weeklyCheckDayTimesViewModels();
                }

                this.validate = false;
                this.raisePropertyChangedAllWeekly();
                this.validate = true;
                this.weeklyAdjustControlEnablement();
            }
        }

        public bool WeeklyMondayActive
        {
            get => this.weeklyDays[0];
            set => this.weeklyDays[0] = value;
        }

        public bool WeeklyTuesdayActive
        {
            get => this.weeklyDays[1];
            set => this.weeklyDays[1] = value;
        }
        public bool WeeklyWednesdayActive
        {
            get => this.weeklyDays[2];
            set => this.weeklyDays[2] = value;
        }

        public bool WeeklyThursdayActive
        {
            get => this.weeklyDays[3];
            set => this.weeklyDays[3] = value;
        }
        public bool WeeklyFridayActive
        {
            get => this.weeklyDays[4];
            set => this.weeklyDays[4] = value;
        }
        public bool WeeklySaturdayActive
        {
            get => this.weeklyDays[5];
            set => this.weeklyDays[5] = value;
        }
        public bool WeeklySundayActive
        {
            get => this.weeklyDays[6];
            set => this.weeklyDays[6] = value;
        }

        public bool WeeklyMondayTime1Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(0))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyMondayTime2Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(0))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyMondayTime3Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(0))
                {
                    return false;
                }

                if (this.schedule.WeeklyGetDayTime(DayOfWeek.Monday, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyTuesdayTime1Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(1))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyTuesdayTime2Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(1))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyTuesdayTime3Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(1))
                {
                    return false;
                }

                if (this.schedule.WeeklyGetDayTime(DayOfWeek.Tuesday, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }
        public bool WeeklyWednesdayTime1Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(2))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyWednesdayTime2Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(2))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyWednesdayTime3Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(2))
                {
                    return false;
                }

                if (this.schedule.WeeklyGetDayTime(DayOfWeek.Wednesday, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyThursdayTime1Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(3))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyThursdayTime2Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(3))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyThursdayTime3Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(3))
                {
                    return false;
                }

                if (this.schedule.WeeklyGetDayTime(DayOfWeek.Thursday, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyFridayTime1Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(4))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyFridayTime2Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(4))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklyFridayTime3Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(4))
                {
                    return false;
                }

                if (this.schedule.WeeklyGetDayTime(DayOfWeek.Friday, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklySaturdayTime1Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(5))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklySaturdayTime2Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(5))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklySaturdayTime3Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(5))
                {
                    return false;
                }

                if (this.schedule.WeeklyGetDayTime(DayOfWeek.Saturday, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklySundayTime1Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(6))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklySundayTime2Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(6))
                {
                    return false;
                }

                return true;
            }
        }

        public bool WeeklySundayTime3Enabled
        {
            get
            {
                if (!this.schedule.WeeklyHasAdvancedSchedule)
                {
                    return false;
                }

                if (!this.schedule.WeeklyGetWeekDayActive(6))
                {
                    return false;
                }

                if (this.schedule.WeeklyGetDayTime(DayOfWeek.Sunday, 1) == null)
                {
                    return false;
                }

                return true;
            }
        }


        public QuarterHourViewModel WeeklyMondayTime1
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Monday))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Monday][0]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Monday][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyMondayTime2
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Monday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Monday][1]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Monday][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyMondayTime3
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Monday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Monday][2]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Monday][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyTuesdayTime1
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Tuesday))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Tuesday][0]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Tuesday][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyTuesdayTime2
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Tuesday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Tuesday][1]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Tuesday][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyTuesdayTime3
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Tuesday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Tuesday][2]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Tuesday][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyWednesdayTime1
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Wednesday))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Wednesday][0]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Wednesday][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyWednesdayTime2
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Wednesday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Wednesday][1]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Wednesday][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyWednesdayTime3
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Wednesday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Wednesday][2]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Wednesday][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyThursdayTime1
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Thursday))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Thursday][0]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Thursday][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyThursdayTime2
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Thursday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Thursday][1]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Thursday][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyThursdayTime3
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Thursday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Thursday][2]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Thursday][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyFridayTime1
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Friday))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Friday][0]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Friday][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyFridayTime2
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Friday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Friday][1]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Friday][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklyFridayTime3
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Friday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Friday][2]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Friday][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklySaturdayTime1
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Saturday))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Saturday][0]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Saturday][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklySaturdayTime2
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Saturday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Saturday][1]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Saturday][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklySaturdayTime3
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Saturday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Saturday][2]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Saturday][2] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklySundayTime1
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Sunday))
                {
                    return null;
                }

                return this.quarterHourViewModels.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Sunday][0]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Sunday][0] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklySundayTime2
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Sunday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Sunday][1]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Sunday][1] = value.QuarterHour;
            }
        }

        public QuarterHourViewModel WeeklySundayTime3
        {
            get
            {
                if (!this.weeklyDayTimes.ContainsKey(DayOfWeek.Sunday))
                {
                    return null;
                }

                return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(this.weeklyDayTimes[DayOfWeek.Sunday][2]);
            }
            set
            {
                this.weeklyDayTimes[DayOfWeek.Sunday][2] = value.QuarterHour;
            }
        }

        #endregion

        #region monthly
        public int MonthlyMonthFrequency
        {
            get => this.schedule.MonthlyMonthFrequency;
            set
            {
                this.schedule.MonthlyMonthFrequency = value;
                this.raisePropertyChanged("MonthlyMonthFrequency");
            }
        }
        public QuarterHourViewModel MonthlyDefaultTime
        {
            get
            {
                return this.quarterHourViewModels.GetQuarterHourViewModel(this.schedule.MonthlyDefaultTime);
            }
            set
            {
                this.schedule.MonthlyDefaultTime = value.QuarterHour.Value;
                this.raisePropertyChanged("MonthlyDefaultTime");
            }
        }

        public bool MonthlyDefaultTimeEnabled
        {
            get
            {
                return !this.schedule.MonthlyHasAdvancedSchedule;
            }
        }

        public bool MonthlyHasAdvancedSchedule
        {
            get
            {
                return this.schedule.MonthlyHasAdvancedSchedule;
            }

            set
            {
                this.schedule.MonthlyHasAdvancedSchedule = value;

                //if advanced day times didn't contain activated days yet.
                if (value)
                {
                    if (this.schedule.MonthlyDateBased)
                    {
                        this.monthlyMonthDateBasedCheckDayTimesViewModels();
                    }
                    else
                    {
                        this.monthlyMonthRelativeBasedCheckDayTimesViewModels();
                    }
                }

                this.validate = false;
                this.raisePropertyChangedAllMonthly();
                this.validate = true;

                this.monthlyAdjustControlEnablement();
            }
        }

        public bool MonthlyTime1Enabled
        {
            get
            {
                if (!this.schedule.MonthlyHasAdvancedSchedule)
                {
                    return false;
                }

                if (this.schedule.MonthlyDateBased)
                {
                    return this.schedule.MonthlyMonthDateBasedGetDayActive(this.monthlyMonthDateBasedDay.Day - 1);
                }
                else
                {
                    return  this.schedule.MonthlyMonthRelativeBasedCombinations.Where(
                        mrc => mrc.DayPosition == this.monthlyMonthRelativeBasedDayPosition && mrc.DayOfWeek == this.monthlyMonthRelativeBasedDay).ToArray().Length > 0;
                }
            }
        }

        public bool MonthlyTime2Enabled
        {
            get
            {
                if (!this.schedule.MonthlyHasAdvancedSchedule)
                {
                    return false;
                }

                if (this.schedule.MonthlyDateBased)
                {
                    return this.schedule.MonthlyMonthDateBasedGetDayActive(this.monthlyMonthDateBasedDay.Day - 1);
                }
                else
                {
                    return this.schedule.MonthlyMonthRelativeBasedCombinations.Where(
                        mrc => mrc.DayPosition == this.monthlyMonthRelativeBasedDayPosition && mrc.DayOfWeek == this.monthlyMonthRelativeBasedDay).ToArray().Length > 0;
                }
            }
        }

        public bool MonthlyTime3Enabled
        {
            get
            {
                if (!this.schedule.MonthlyHasAdvancedSchedule)
                {
                    return false;
                }

                if (this.schedule.MonthlyDateBased)
                {
                    return this.schedule.MonthlyMonthDateBasedGetDayActive(this.monthlyMonthDateBasedDay.Day - 1) && this.schedule.MonthlyMonthDateBasedGetDayTime(this.monthlyMonthDateBasedDay.Day - 1, 1) != null;
                }
                else
                {
                    return this.schedule.MonthlyMonthRelativeBasedCombinations.Where(
                        mrc => mrc.DayPosition == this.monthlyMonthRelativeBasedDayPosition && mrc.DayOfWeek == this.monthlyMonthRelativeBasedDay).ToArray().Length > 0 && 
                           this.schedule.MonthlyMonthRelativeBasedGetDayTime(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay, 1) != null;
                }
            }
        }

        public QuarterHourViewModel MonthlyTime1
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    if(this.monthlyMonthDateBasedDayTimes.ContainsKey(this.monthlyMonthDateBasedDay.Day - 1))
                    {
                        return this.quarterHourViewModels.GetQuarterHourViewModel(
                            this.monthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][0]);
                    }

                    return null;
                }
                else
                {
                    TimeSpan?[] timeSpans = this.getMonthRelativeBasedTimes(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                    if (timeSpans == null)
                    {
                        return null;
                    }

                    return this.quarterHourViewModels.GetQuarterHourViewModel(timeSpans[0]);
                }
            }

            set
            {
                if (this.schedule.MonthlyDateBased)
                {
                    this.monthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][0] = value.QuarterHour;
                }
                else
                {
                    TimeSpan?[] timeSpans =
                        this.getMonthRelativeBasedTimes(this.monthlyMonthRelativeBasedDayPosition,
                            this.monthlyMonthRelativeBasedDay);
                    timeSpans[0] = value.QuarterHour;
                }
            }
        }

        public QuarterHourViewModel MonthlyTime2
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    if (this.monthlyMonthDateBasedDayTimes.ContainsKey(this.monthlyMonthDateBasedDay.Day - 1))
                    {
                        return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(
                            this.monthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][1]);
                    }

                    return null;
                }
                else
                {
                    TimeSpan?[] timeSpans = this.getMonthRelativeBasedTimes(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                    if (timeSpans == null)
                    {
                        return null;
                    }

                    return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(timeSpans[1]);
                }
            }

            set
            {
                if (this.schedule.MonthlyDateBased)
                {
                    this.monthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][1] = value.QuarterHour;
                }
                else
                {
                    TimeSpan?[] timeSpans =
                        this.getMonthRelativeBasedTimes(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                    timeSpans[1] = value.QuarterHour;
                }
            }
        }

        public QuarterHourViewModel MonthlyTime3
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    if (this.monthlyMonthDateBasedDayTimes.ContainsKey(this.monthlyMonthDateBasedDay.Day - 1))
                    {
                        return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(
                            this.monthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][2]);
                    }

                    return null;
                }
                else
                {
                    TimeSpan?[] timeSpans = this.getMonthRelativeBasedTimes(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                    if (timeSpans == null)
                    {
                        return null;
                    }

                    return this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(timeSpans[2]);
                }
            }

            set
            {
                if (this.schedule.MonthlyDateBased)
                {
                    this.monthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][2] = value.QuarterHour;
                }
                else
                {
                    TimeSpan?[] timeSpans =
                        this.getMonthRelativeBasedTimes(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                    timeSpans[2] = value.QuarterHour;
                }
            }
        }

        private TimeSpan?[] getMonthRelativeBasedTimes(DayPosition dayPosition, DayOfWeek day)
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

            return this.monthlyMonthRelativeBasedDayTimes[combinations[0]];
        }

        private MonthRelativeCombination monthlyMonthRelativeBasedGetCombination(DayPosition dayPosition, DayOfWeek day)
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

        public List<MonthDayViewModel> MonthlyMonthDateBasedDayViewModels
        {
            get => this.monthlyMonthDateBasedDayViewModels;
        }

        public MonthDayViewModel MonthlyMonthDateBasedDay
        {
            get => this.monthlyMonthDateBasedDay;
            set
            {
                this.monthlyMonthDateBasedDay = value;
                this.raisePropertyChanged("MonthlyMonthDateBasedDay");
                this.validate = false;
                this.raisePropertyChanged("MonthlyTime1");
                this.raisePropertyChanged("MonthlyTime2");
                this.raisePropertyChanged("MonthlyTime3");
                this.raisePropertyChanged("MonthlyActive");
                this.validate = true;
                this.monthlyAdjustControlEnablement();
            }
        }

        public ObservableCollection<MonthRelativeCombinationViewModel> MonthlyMonthRelativeBasedCombinationViewModels
        {
            get => this.monthlyMonthRelativeBasedCombinationViewModels;
        }

        public MonthRelativeCombinationViewModel MonthlyMonthRelativeBasedCombination
        {
            get => this.monthlyMonthRelativeBasedCombination;
            set
            {
                this.monthlyMonthRelativeBasedCombination = value;
                this.monthlyMonthRelativeBasedDayPosition = value.DayPosition;
                this.monthlyMonthRelativeBasedDay = value.DayOfWeek;

                this.validate = false;
                this.raisePropertyChanged("MonthlyTime1");
                this.raisePropertyChanged("MonthlyTime2");
                this.raisePropertyChanged("MonthlyTime3");
                this.raisePropertyChanged("MonthlyMonthRelativeBasedDayPosition");
                this.raisePropertyChanged("MonthlyMonthRelativeBasedDay");
                this.raisePropertyChanged("MonthlyActive");
                this.validate = true;
                this.monthlyAdjustControlEnablement();
            }
        }

        public Array MonthlyMonthRelativeBasedDays
        {
            get
            {
                return Enum.GetValues(typeof(DayOfWeek));
            }
        }
        public Array MonthlyMonthRelativeBasedDayPositions
        {
            get
            {
                return Enum.GetValues(typeof(DayPosition));
            }
        }

        public DayOfWeek MonthlyMonthRelativeBasedDay
        {
            get => this.monthlyMonthRelativeBasedDay;
            set
            {
                this.monthlyMonthRelativeBasedDay = value;
                this.validate = false;
                this.raisePropertyChanged("MonthlyTime1");
                this.raisePropertyChanged("MonthlyTime2");
                this.raisePropertyChanged("MonthlyTime3");
                this.raisePropertyChanged("MonthlyMonthRelativeBasedDay");
                this.raisePropertyChanged("MonthlyActive");
                this.validate = true;
                this.monthlyAdjustControlEnablement();
            }
        }

        public DayPosition MonthlyMonthRelativeBasedDayPosition
        {
            get => this.monthlyMonthRelativeBasedDayPosition;
            set
            {
                this.monthlyMonthRelativeBasedDayPosition = value;
                this.validate = false;
                this.raisePropertyChanged("MonthlyTime1");
                this.raisePropertyChanged("MonthlyTime2");
                this.raisePropertyChanged("MonthlyTime3");
                this.raisePropertyChanged("MonthlyMonthRelativeBasedDayPosition");
                this.raisePropertyChanged("MonthlyActive");
                this.validate = true;
                this.monthlyAdjustControlEnablement();
            }
        }

        public bool MonthlyActive
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    return this.monthlyMonthDateBasedDates[this.monthlyMonthDateBasedDay.Day - 1];
                }
                else
                {
                    if (this.monthlyMonthRelativeBasedCombinations.Exists(
                        combi => combi.DayPosition == this.monthlyMonthRelativeBasedDayPosition && combi.DayOfWeek == this.monthlyMonthRelativeBasedDay))
                    {
                        return true;
                    }

                    return false;
                }

            }
            set
            {
                if (this.schedule.MonthlyDateBased)
                {
                    int index = this.monthlyMonthDateBasedDay.Day - 1;
                    this.monthlyMonthDateBasedDayViewModels[index].Active = value;
                    this.monthlyMonthDateBasedDates[index] = value;
                    this.raisePropertyChanged("MonthlyActive");
                }
                else
                {
                    if (value)
                    {
                        if (!this.monthlyMonthRelativeBasedCombinations.Exists(
                            combi => combi.DayPosition == this.monthlyMonthRelativeBasedDayPosition && combi.DayOfWeek == this.monthlyMonthRelativeBasedDay))
                        {
                            MonthRelativeCombination monthRelativeCombination = this.monthlyMonthRelativeBasedGetKeyFromDayTimes(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                            if (monthRelativeCombination == null)
                            {
                                monthRelativeCombination = new MonthRelativeCombination(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                            }

                            this.monthlyMonthRelativeBasedCombinations.Add(monthRelativeCombination);
                        }
                    }
                    else
                    {
                        MonthRelativeCombination monthRelativeCombination = this.monthlyMonthRelativeBasedGetCombination(
                                this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                        this.monthlyMonthRelativeBasedCombinations.Remove(monthRelativeCombination);
                    }
                }
            }
        }

        public int MonthlyMonthDateBasedIndex
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            set
            {
                if (value == 0)
                {
                    this.schedule.MonthlyDateBased = true;
                    if (this.schedule.MonthlyHasAdvancedSchedule)
                    {
                        this.monthlyMonthDateBasedCheckDayTimesViewModels();
                    }
                }
                else
                {
                    this.schedule.MonthlyDateBased = false;
                    if (this.schedule.MonthlyHasAdvancedSchedule)
                    {
                        this.monthlyMonthRelativeBasedCheckDayTimesViewModels();
                    }
                }

                this.validate = false;
                this.raisePropertyChangedAllMonthly();
                this.validate = true;
            }
        }

        public string MonthlyMonthDateBasedVisible
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public string MonthlyMonthRelativeBasedVisible
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    return "Collapsed";
                }
                else
                {
                    return "Visible";
                }
            }
        }
        #endregion

        public PublishAtScheduleViewModel(Schedule schedule)
        {
            this.resetCommand = new GenericCommand(this.resetAllSchedules);

            if (schedule != null)
            {
                this.schedule = new Schedule(schedule);
            }
            else
            {
                this.schedule = new Schedule();
            }

            this.quarterHourViewModelsEmptyStartValue = new QuarterHourViewModels(true);
            this.quarterHourViewModels = new QuarterHourViewModels(false);

            this.initializeSpecificDateViewModels();
            this.initializeDailyViewModels();
            this.initializeWeeklyViewModels();
            this.initializeMonthlyMonthDateBasedDayViewModels();
            this.initializeMonthlyMonthRelativeViewModels();
        }

        private void initializeSpecificDateViewModels()
        {
            this.specificDateDateTime = this.schedule.SpecificDateDateTime;
        }

        private void initializeDailyViewModels()
        {
            this.dailyDays = new bool[3];
            for (int day = 0; day < 3; day++)
            {
                this.dailyDays[day] = this.schedule.DailyGetDayActive(day);
            }

            this.dailyDayTimes = new Dictionary<int, TimeSpan?[]>();

            foreach (int dayIndex in this.schedule.DailyDaysWithDayTimes)
            {
                TimeSpan?[] times = new TimeSpan?[3];
                for (int slot = 0; slot < 3; slot++)
                {
                    times[slot] = this.schedule.DailyGetDayTime(dayIndex, slot);
                }

                this.dailyDayTimes.Add(dayIndex, times);
            }
        }

        private void initializeWeeklyViewModels()
        {
            this.weeklyDays = new bool[7];
            for (int day = 0; day < 7; day++)
            {
                this.weeklyDays[day] = this.schedule.WeeklyGetWeekDayActive(day);
            }

            this.weeklyDayTimes = new Dictionary<DayOfWeek, TimeSpan?[]>();

            foreach (DayOfWeek dayOfWeek in this.schedule.WeeklyDaysWithDayTimes)
            {
                TimeSpan?[] times = new TimeSpan?[3];
                for (int slot = 0; slot < 3; slot++)
                {
                    times[slot] = this.schedule.WeeklyGetDayTime(dayOfWeek, slot);
                }

                this.weeklyDayTimes.Add(dayOfWeek, times);
            }
        }

        private void initializeMonthlyMonthDateBasedDayViewModels()
        {
            this.monthlyMonthDateBasedDates = new bool[31];
            this.monthlyMonthDateBasedDayViewModels = new List<MonthDayViewModel>(31);
            this.monthlyMonthDateBasedDayTimes = new Dictionary<int, TimeSpan?[]>();
            for (int dayIndex = 0; dayIndex <= 30; dayIndex++)
            {
                this.monthlyMonthDateBasedDates[dayIndex] = this.schedule.MonthlyMonthDateBasedGetDayActive(dayIndex);

                MonthDayViewModel monthDayViewModel = new MonthDayViewModel(dayIndex + 1);
                if (this.monthlyMonthDateBasedDates[dayIndex])
                {
                    monthDayViewModel.Active = true;
                }

                if (this.schedule.MonthlyMonthDateBasedDaysWithDayTimes.Contains(dayIndex))
                {
                    TimeSpan?[] times = new TimeSpan?[3];
                    for (int timeIndex = 0; timeIndex < 3; timeIndex++)
                    {
                        times[timeIndex] = this.schedule.MonthlyMonthDateBasedGetDayTime(dayIndex, timeIndex);
                    }

                    this.monthlyMonthDateBasedDayTimes.Add(dayIndex, times);
                }

                this.monthlyMonthDateBasedDayViewModels.Add(monthDayViewModel);
            }

            this.monthlyMonthDateBasedDay = this.monthlyMonthDateBasedDayViewModels.Find(vm => vm.Day == 1);
        }

        private void initializeMonthlyMonthRelativeViewModels()
        {
            this.monthlyMonthRelativeBasedCombinations = new List<MonthRelativeCombination>();
            this.monthlyMonthRelativeBasedDayTimes = new Dictionary<MonthRelativeCombination, TimeSpan?[]>();
            this.monthlyMonthRelativeBasedCombinationViewModels = new ObservableCollection<MonthRelativeCombinationViewModel>();
            foreach (MonthRelativeCombination monthRelativeCombination in this.schedule.MonthlyMonthRelativeBasedCombinations)
            {
                MonthRelativeCombination monthRelativeCombinationInner = new MonthRelativeCombination(
                    monthRelativeCombination.DayPosition, monthRelativeCombination.DayOfWeek);
                this.monthlyMonthRelativeBasedCombinations.Add(monthRelativeCombinationInner);
                this.monthlyMonthRelativeBasedCombinationViewModels.Add(new MonthRelativeCombinationViewModel(monthRelativeCombinationInner));
            }

            foreach (MonthRelativeCombination monthRelativeCombinationSchedule in this.schedule.MonthlyMonthRelativeBasedCombinationsWithDayTimes)
            {
                MonthRelativeCombination monthRelativeCombination =
                    this.monthlyMonthRelativeBasedGetCombination(monthRelativeCombinationSchedule.DayPosition, monthRelativeCombinationSchedule.DayOfWeek);

                if (monthRelativeCombination == null)
                {
                    monthRelativeCombination = new MonthRelativeCombination(monthRelativeCombinationSchedule.DayPosition, monthRelativeCombinationSchedule.DayOfWeek);
                }

                TimeSpan?[] newTimeSpans = new TimeSpan?[3];
                for (int index = 0; index < 3; index++)
                {
                    newTimeSpans[index] = this.schedule.MonthlyMonthRelativeBasedGetDayTime(
                        monthRelativeCombinationSchedule.DayPosition, monthRelativeCombinationSchedule.DayOfWeek, index);
                }

                this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombination, newTimeSpans);
            }

            this.monthlyMonthRelativeBasedCombination = this.monthlyMonthRelativeBasedCombinationViewModels[0];
            this.monthlyMonthRelativeBasedDayPosition = this.monthlyMonthRelativeBasedCombinationViewModels[0].DayPosition;
            this.monthlyMonthRelativeBasedDay = this.monthlyMonthRelativeBasedCombinationViewModels[0].DayOfWeek;
        }

        private void resetAllSchedules(object obj)
        {
            this.validate = false;
            this.schedule.Reset();
            this.initializeDailyViewModels();
            this.initializeWeeklyViewModels();
            this.initializeMonthlyMonthDateBasedDayViewModels();
            this.initializeMonthlyMonthRelativeViewModels();
            this.dailyAdjustControlEnablement();
            this.weeklyAdjustControlEnablement();
            this.monthlyAdjustControlEnablement();

            this.raisePropertyChanged("ScheduleFrequency");

            this.raisePropertyChangedAllDaily();
            this.raisePropertyChangedAllWeekly();
            this.raisePropertyChangedAllMonthly();

            this.validate = true;
        }

        private void raisePropertyChangedAllMonthly()
        {
            this.raisePropertyChanged("MonthlyMonthFrequency");
            this.raisePropertyChanged("MonthlyDefaultTime");
            this.raisePropertyChanged("MonthlyMonthDateBasedIndex");
            this.raisePropertyChanged("MonthlyMonthDateBasedVisible");
            this.raisePropertyChanged("MonthlyMonthRelativeBasedVisible"); 
            this.raisePropertyChanged("MonthlyMonthRelativeBasedCombination");
            this.raisePropertyChanged("MonthlyMonthRelativeBasedCombinationViewModels");
            this.raisePropertyChanged("MonthlyHasAdvancedSchedule");
            this.raisePropertyChanged("MonthlyActive");
            this.raisePropertyChanged("MonthlyTime1");
            this.raisePropertyChanged("MonthlyTime2");
            this.raisePropertyChanged("MonthlyTime3");
        }

        private void raisePropertyChangedAllWeekly()
        {
            this.raisePropertyChanged("WeeklyWeekFrequency");
            this.raisePropertyChanged("WeeklyDefaultTime");
            this.raisePropertyChanged("WeeklyHasAdvancedSchedule");
            this.raisePropertyChanged("WeeklyMondayActive");
            this.raisePropertyChanged("WeeklyTuesdayActive");
            this.raisePropertyChanged("WeeklyWednesdayActive");
            this.raisePropertyChanged("WeeklyThursdayActive");
            this.raisePropertyChanged("WeeklyFridayActive");
            this.raisePropertyChanged("WeeklySaturdayActive");
            this.raisePropertyChanged("WeeklySundayActive");
            this.raisePropertyChanged("WeeklyMondayTime1");
            this.raisePropertyChanged("WeeklyMondayTime2");
            this.raisePropertyChanged("WeeklyMondayTime3");
            this.raisePropertyChanged("WeeklyTuesdayTime1");
            this.raisePropertyChanged("WeeklyTuesdayTime2");
            this.raisePropertyChanged("WeeklyTuesdayTime3");
            this.raisePropertyChanged("WeeklyWednesdayTime1");
            this.raisePropertyChanged("WeeklyWednesdayTime2");
            this.raisePropertyChanged("WeeklyWednesdayTime3");
            this.raisePropertyChanged("WeeklyThursdayTime1");
            this.raisePropertyChanged("WeeklyThursdayTime2");
            this.raisePropertyChanged("WeeklyThursdayTime3");
            this.raisePropertyChanged("WeeklyFridayTime1");
            this.raisePropertyChanged("WeeklyFridayTime2");
            this.raisePropertyChanged("WeeklyFridayTime3");
            this.raisePropertyChanged("WeeklySaturdayTime1");
            this.raisePropertyChanged("WeeklySaturdayTime2");
            this.raisePropertyChanged("WeeklySaturdayTime3");
            this.raisePropertyChanged("WeeklySundayTime1");
            this.raisePropertyChanged("WeeklySundayTime2");
            this.raisePropertyChanged("WeeklySundayTime3");
        }

        private void raisePropertyChangedAllDaily()
        {
            this.raisePropertyChanged("DailyDayFrequency");
            this.raisePropertyChanged("DailyDefaultTime");
            this.raisePropertyChanged("DailyHasAdvancedSchedule");
            this.raisePropertyChanged("DailyDay2Active");
            this.raisePropertyChanged("DailyDay3Active");
            this.raisePropertyChanged("DailyDay1Time1");
            this.raisePropertyChanged("DailyDay1Time2");
            this.raisePropertyChanged("DailyDay1Time3");
            this.raisePropertyChanged("DailyDay2Time1");
            this.raisePropertyChanged("DailyDay2Time2");
            this.raisePropertyChanged("DailyDay2Time3");
            this.raisePropertyChanged("DailyDay3Time1");
            this.raisePropertyChanged("DailyDay3Time2");
            this.raisePropertyChanged("DailyDay3Time3");
        }

        private void dailyAdjustControlEnablement()
        {
            this.raisePropertyChanged("DailyDefaultTimeEnabled");
            this.raisePropertyChanged("DailyDay2ActiveEnabled");
            this.raisePropertyChanged("DailyDay3ActiveEnabled");
            this.raisePropertyChanged("DailyDay1Time1Enabled");
            this.raisePropertyChanged("DailyDay1Time2Enabled");
            this.raisePropertyChanged("DailyDay1Time3Enabled");
            this.raisePropertyChanged("DailyDay2Time1Enabled");
            this.raisePropertyChanged("DailyDay2Time2Enabled");
            this.raisePropertyChanged("DailyDay2Time3Enabled");
            this.raisePropertyChanged("DailyDay3Time1Enabled");
            this.raisePropertyChanged("DailyDay3Time2Enabled");
            this.raisePropertyChanged("DailyDay3Time3Enabled");
        }

        private void weeklyAdjustControlEnablement()
        {
            this.raisePropertyChanged("WeeklyDefaultTimeEnabled");
            this.raisePropertyChanged("WeeklyMondayTime1Enabled");
            this.raisePropertyChanged("WeeklyMondayTime2Enabled");
            this.raisePropertyChanged("WeeklyMondayTime3Enabled");
            this.raisePropertyChanged("WeeklyTuesdayTime1Enabled");
            this.raisePropertyChanged("WeeklyTuesdayTime2Enabled");
            this.raisePropertyChanged("WeeklyTuesdayTime3Enabled");
            this.raisePropertyChanged("WeeklyWednesdayTime1Enabled");
            this.raisePropertyChanged("WeeklyWednesdayTime2Enabled");
            this.raisePropertyChanged("WeeklyWednesdayTime3Enabled");
            this.raisePropertyChanged("WeeklyThursdayTime1Enabled");
            this.raisePropertyChanged("WeeklyThursdayTime2Enabled");
            this.raisePropertyChanged("WeeklyThursdayTime3Enabled");
            this.raisePropertyChanged("WeeklyFridayTime1Enabled");
            this.raisePropertyChanged("WeeklyFridayTime2Enabled");
            this.raisePropertyChanged("WeeklyFridayTime3Enabled");
            this.raisePropertyChanged("WeeklySaturdayTime1Enabled");
            this.raisePropertyChanged("WeeklySaturdayTime2Enabled");
            this.raisePropertyChanged("WeeklySaturdayTime3Enabled");
            this.raisePropertyChanged("WeeklySundayTime1Enabled");
            this.raisePropertyChanged("WeeklySundayTime2Enabled");
            this.raisePropertyChanged("WeeklySundayTime3Enabled");
        }
        private void monthlyAdjustControlEnablement()
        {
            this.raisePropertyChanged("MonthlyDefaultTimeEnabled");
            this.raisePropertyChanged("MonthlyTime1Enabled");
            this.raisePropertyChanged("MonthlyTime2Enabled");
            this.raisePropertyChanged("MonthlyTime3Enabled");
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string this[string propertyName]
        {
            get
            {
                if (!this.validate)
                {
                    return string.Empty;
                }

                switch (propertyName)
                {
                    #region specific
                    case "SpecificDateDate":
                    case "SpecificDateTime":
                        return this.validateSpecificDate(propertyName);
                    #endregion

                    #region daily
                    case "DailyDay1Time1":
                    case "DailyDay1Time2":
                    case "DailyDay1Time3":
                    case "DailyDay2Time1":
                    case "DailyDay2Time2":
                    case "DailyDay2Time3":
                    case "DailyDay3Time1":
                    case "DailyDay3Time2":
                    case "DailyDay3Time3":
                        return this.validateDailyTimes(propertyName);
                    case "DailyDay2Active":
                    case "DailyDay3Active":
                        return this.validateDailyActive(propertyName);
                    #endregion daily
                    #region weekly
                    case "WeeklyMondayActive":
                    case "WeeklyTuesdayActive":
                    case "WeeklyWednesdayActive":
                    case "WeeklyThursdayActive":
                    case "WeeklyFridayActive":
                    case "WeeklySaturdayActive":
                    case "WeeklySundayActive":
                        return this.validateWeeklyActive(propertyName);
                    case "WeeklyMondayTime1":
                    case "WeeklyMondayTime2":
                    case "WeeklyMondayTime3":
                    case "WeeklyTuesdayTime1":
                    case "WeeklyTuesdayTime2":
                    case "WeeklyTuesdayTime3":
                    case "WeeklyWednesdayTime1":
                    case "WeeklyWednesdayTime2":
                    case "WeeklyWednesdayTime3":
                    case "WeeklyThursdayTime1":
                    case "WeeklyThursdayTime2":
                    case "WeeklyThursdayTime3":
                    case "WeeklyFridayTime1":
                    case "WeeklyFridayTime2":
                    case "WeeklyFridayTime3":
                    case "WeeklySaturdayTime1":
                    case "WeeklySaturdayTime2":
                    case "WeeklySaturdayTime3":
                    case "WeeklySundayTime1":
                    case "WeeklySundayTime2":
                    case "WeeklySundayTime3":
                        return this.validateWeeklyTimes(propertyName);
                    #endregion weekly

                    #region monthly
                    case "MonthlyActive":
                        return this.validateMonthlyActive(propertyName);
                    case "MonthlyTime1":
                    case "MonthlyTime2":
                    case "MonthlyTime3":
                        return this.validateMonthlyTimes(propertyName);
                    #endregion monthly
                    default:
                        throw new ArgumentException("Unknown property to validate.");
                }
            }
        }

        private string validateSpecificDate(string propertyName)
        {
            try
            {
                if (propertyName == "SpecificDateDate")
                {
                    this.schedule.SpecificDateDateTime = this.specificDateDateTime.Date.Add(this.schedule.SpecificDateDateTime.TimeOfDay);
                }
                else
                {
                    this.schedule.SpecificDateDateTime = this.schedule.SpecificDateDateTime.Date.Add(this.specificDateDateTime.TimeOfDay);
                }

                this.validate = false;
                this.raisePropertyChanged(propertyName);
                this.validate = true;
                return string.Empty;
            }
            catch (InvalidScheduleException e)
            {
                return e.Message;
            }
        }

        private string validateDailyActive(string propertyName)
        {
            int dayIndex = Convert.ToInt32(propertyName.Substring(8, 1)) - 1;

            try
            {
                this.schedule.DailySetDayActive(dayIndex, this.dailyDays[dayIndex]);

                //if a day was set to active, ensure view's data also contains times array for this day.
                //if a schedule is newly created, time time arrays of the 2nd and 3rd day don't exist,
                //arrays will be added when a day is activated for the first time.
                if (dayIndex > 0 && this.dailyDays[dayIndex])
                {
                    if (this.schedule.DailyHasAdvancedSchedule)
                    {
                        if (!this.dailyDayTimes.ContainsKey(dayIndex))
                        {
                            TimeSpan?[] times = new TimeSpan?[3];
                            for (int timeIndex = 0; timeIndex <= 2; timeIndex++)
                            {
                                times[timeIndex] = this.schedule.DailyGetDayTime(dayIndex, timeIndex);
                            }

                            this.dailyDayTimes.Add(dayIndex, times);
                        }
                    }
                }

                this.validate = false;
                this.raisePropertyChanged(propertyName);

                //if a new times array was added for this day, ensure that data of first time slot is shown.
                this.raisePropertyChanged("DailyDay2Time1");
                this.raisePropertyChanged("DailyDay3Time1");
                this.validate = true;
                this.dailyAdjustControlEnablement();
                return string.Empty;
            }
            catch (InvalidScheduleException e)
            {
                return e.Message;
            }
        }

        private string validateWeeklyActive(string propertyName)
        {
            string dayString = propertyName.Substring(6, propertyName.Length - 12);
            DayOfWeek dayOfWeek = (DayOfWeek) Enum.Parse(typeof(DayOfWeek), dayString);
            int dayIndex = Schedule.GetCorrectedDayIndex(dayOfWeek);

            try
            {
                this.schedule.WeeklySetDayActive(dayIndex, this.weeklyDays[dayIndex]);

                if (this.schedule.WeeklyHasAdvancedSchedule)
                {
                    if (!this.weeklyDayTimes.ContainsKey(dayOfWeek))
                    {
                        TimeSpan?[] times = new TimeSpan?[3];
                        for (int timeIndex = 0; timeIndex <= 2; timeIndex++)
                        {
                            times[timeIndex] = this.schedule.WeeklyGetDayTime(dayOfWeek, timeIndex);
                        }

                        this.weeklyDayTimes.Add(dayOfWeek, times);
                    }
                }

                this.validate = false;
                this.raisePropertyChanged(propertyName);
                this.raisePropertyChanged($"Weekly{dayString}Time1");
                this.raisePropertyChanged($"Weekly{dayString}Time2");
                this.raisePropertyChanged($"Weekly{dayString}Time3");
                this.validate = true;
                this.weeklyAdjustControlEnablement();
                return string.Empty;
            }
            catch (InvalidScheduleException e)
            {
                return e.Message;
            }
        }

        private string validateDailyTimes(string propertyName)
        {
            int dayIndex = Convert.ToInt32(propertyName.Substring(8, 1)) - 1;
            int timeIndex = Convert.ToInt32(propertyName.Substring(13, 1)) - 1;
            try
            {
                this.schedule.DailySetDayTime(dayIndex, timeIndex, this.dailyDayTimes[dayIndex][timeIndex]);
                this.validate = false;
                this.raisePropertyChanged(propertyName);
                this.validate = true;
                this.dailyAdjustControlEnablement();
                return string.Empty;
            }
            catch (InvalidScheduleException e)
            {
                return e.Message;
            }
        }

        private string validateWeeklyTimes(string propertyName)
        {
            string dayString = propertyName.Substring(6, propertyName.Length - 11);
            DayOfWeek day = (DayOfWeek) Enum.Parse(typeof(DayOfWeek), dayString);
            int timeIndex = Convert.ToInt32(propertyName.Substring(propertyName.Length - 1, 1)) - 1;
            
            try
            {
                this.schedule.WeeklySetDayTime(day, timeIndex, this.weeklyDayTimes[day][timeIndex]);
                this.validate = false;
                this.raisePropertyChanged(propertyName);
                this.validate = true;
                this.weeklyAdjustControlEnablement();
                return string.Empty;
            }
            catch (InvalidScheduleException e)
            {
                return e.Message;
            }
        }

        private void weeklyCheckDayTimesViewModels()
        {
            for (int dayIndex = 0; dayIndex < 7; dayIndex++)
            {
                if (this.schedule.WeeklyGetWeekDayActive(dayIndex))
                {
                    DayOfWeek dayOfWeek = Schedule.GetDayOfWeekFromDayIndex(dayIndex);
                    if (!this.weeklyDayTimes.ContainsKey(dayOfWeek))
                    {
                        TimeSpan?[] times = new TimeSpan?[3];
                        for (int timeIndex = 0; timeIndex <= 2; timeIndex++)
                        {
                            times[timeIndex] = this.schedule.WeeklyGetDayTime(dayOfWeek, timeIndex);
                        }

                        this.weeklyDayTimes.Add(dayOfWeek, times);
                    }
                }
            }
        }

        private void monthlyMonthRelativeBasedCheckDayTimesViewModels()
        {
            foreach (MonthRelativeCombination monthRelativeCombination in this.schedule.MonthlyMonthRelativeBasedCombinations)
            {
                if (this.monthlyMonthRelativeBasedGetKeyFromDayTimes(
                    monthRelativeCombination.DayPosition, monthRelativeCombination.DayOfWeek) == null)
                {
                    TimeSpan?[] times = new TimeSpan?[3];
                    for (int timeIndex = 0; timeIndex <= 2; timeIndex++)
                    {
                        times[timeIndex] = this.schedule.MonthlyMonthRelativeBasedGetDayTime(
                                monthRelativeCombination.DayPosition, monthRelativeCombination.DayOfWeek, timeIndex);
                    }

                    this.monthlyMonthRelativeBasedDayTimes.Add(this.monthlyMonthRelativeBasedGetCombination(
                        monthRelativeCombination.DayPosition, monthRelativeCombination.DayOfWeek), times);
                }
            }
        }

        private void monthlyMonthDateBasedCheckDayTimesViewModels()
        {
            for (int dayIndex = 0; dayIndex <= 30; dayIndex++)
            {
                if (this.schedule.MonthlyMonthDateBasedGetDayActive(dayIndex))
                {
                    if (!this.monthlyMonthDateBasedDayTimes.ContainsKey(dayIndex))
                    {
                        TimeSpan?[] times = new TimeSpan?[3];
                        for (int timeIndex = 0; timeIndex <= 2; timeIndex++)
                        {
                            times[timeIndex] = this.schedule.MonthlyMonthDateBasedGetDayTime(dayIndex, timeIndex);
                        }

                        this.monthlyMonthDateBasedDayTimes.Add(dayIndex, times);
                    }
                }
            }
        }

        private string validateMonthlyActive(string propertyName)
        {
            if (this.schedule.MonthlyDateBased)
            {
                int dayIndex = this.monthlyMonthDateBasedDay.Day - 1;

                try
                {
                    this.schedule.MonthlyMonthDateBasedSetDayActive(dayIndex, this.monthlyMonthDateBasedDates[dayIndex]);

                    if (this.schedule.MonthlyHasAdvancedSchedule)
                    {
                        if (!this.monthlyMonthDateBasedDayTimes.ContainsKey(dayIndex))
                        {
                            TimeSpan?[] times = new TimeSpan?[3];
                            for (int timeIndex = 0; timeIndex <= 2; timeIndex++)
                            {
                                times[timeIndex] = this.schedule.MonthlyMonthDateBasedGetDayTime(dayIndex, timeIndex);
                            }

                            this.monthlyMonthDateBasedDayTimes.Add(dayIndex, times);
                        }
                    }

                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.raisePropertyChanged("MonthlyTime1");
                    this.raisePropertyChanged("MonthlyTime2");
                    this.raisePropertyChanged("MonthlyTime3");
                    this.validate = true;
                    this.monthlyAdjustControlEnablement();
                    return string.Empty;
                }
                catch (InvalidScheduleException e)
                {
                    //as MonthlyActive state can be change by selection of other controls, we need to revert the change, to prevent inconsistent data.
                    this.monthlyMonthDateBasedDates[dayIndex] = !this.monthlyMonthDateBasedDates[dayIndex];
                    return e.Message;
                }
            }
            else
            {
                try
                {
                    bool active = this.monthlyMonthRelativeBasedCombinations.Exists(
                        combi => combi.DayPosition == this.monthlyMonthRelativeBasedDayPosition && combi.DayOfWeek == this.monthlyMonthRelativeBasedDay);

                    this.schedule.MonthlyMonthRelativeBasedSetDayActive(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay, active);

                    if (!active)
                    {
                        MonthRelativeCombinationViewModel monthRelativeCombinationViewModel = 
                            this.monthlyMonthRelativeBasedGetCombinationViewModel(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                        //to avoid to have an empty combobox selected item. change needs to be raised to view before item is removed.
                        if (this.monthlyMonthRelativeBasedCombination == monthRelativeCombinationViewModel)
                        {
                            if (this.monthlyMonthRelativeBasedCombinationViewModels[0] != monthlyMonthRelativeBasedCombination)
                            {
                                this.monthlyMonthRelativeBasedCombination = this.monthlyMonthRelativeBasedCombinationViewModels[0];
                            }
                            else
                            {
                                this.monthlyMonthRelativeBasedCombination = this.monthlyMonthRelativeBasedCombinationViewModels[1];
                            }
                        }

                        this.raisePropertyChanged("MonthlyMonthRelativeBasedCombination");
                        this.monthlyMonthRelativeBasedCombinationViewModels.Remove(monthRelativeCombinationViewModel);
                    }
                    else
                    {

                        MonthRelativeCombination monthRelativeCombination = this.monthlyMonthRelativeBasedGetCombination(
                                this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                        if (this.schedule.MonthlyHasAdvancedSchedule)
                        {
                            if (!this.monthlyMonthRelativeBasedDayTimes.ContainsKey(monthRelativeCombination))
                            {
                                this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombination, new TimeSpan?[3]);
                                this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombination][0] = new TimeSpan();
                            }
                        }

                        MonthRelativeCombinationViewModel monthRelativeCombinationViewModel = new MonthRelativeCombinationViewModel(monthRelativeCombination);
                        this.monthlyMonthRelativeBasedCombination = monthRelativeCombinationViewModel;
                        this.MonthlyMonthRelativeBasedCombinationViewModels.Add(monthRelativeCombinationViewModel);
                    }


                    this.validate = false;
                    this.raisePropertyChanged(propertyName);

                    this.raisePropertyChanged("MonthlyMonthRelativeBasedCombination");
                    this.raisePropertyChanged("MonthlyMonthRelativeBasedCombinationViewModels");
                    this.raisePropertyChanged("MonthlyTime1");
                    this.raisePropertyChanged("MonthlyTime2");
                    this.raisePropertyChanged("MonthlyTime3");
                    this.validate = true;
                    this.monthlyAdjustControlEnablement();
                    return string.Empty;
                }
                catch (InvalidScheduleException e)
                {
                    //as MonthlyActive state can be change by selection of other controls, we need to revert the change, to prevent inconsistent data.
                    MonthRelativeCombination monthRelativeCombination = this.monthlyMonthRelativeBasedGetCombination(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                    if (monthRelativeCombination == null)
                    {
                        this.monthlyMonthRelativeBasedCombinations.Add(this.monthlyMonthRelativeBasedGetKeyFromDayTimes(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay));
                    }
                    else
                    {
                        this.monthlyMonthRelativeBasedCombinations.Remove(monthRelativeCombination);
                    }

                    return e.Message;
                }
            }
        }

        private string validateMonthlyTimes(string propertyName)
        {
            int timeIndex = Convert.ToInt32(propertyName.Substring(11, 1)) - 1;
            if (this.schedule.MonthlyDateBased)
            {
                try
                {
                    int dayIndex = this.monthlyMonthDateBasedDay.Day - 1;
                    this.schedule.MonthlyMonthDateBasedSetDayTime(dayIndex, timeIndex, this.monthlyMonthDateBasedDayTimes[dayIndex][timeIndex]);
                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.validate = true;
                    this.monthlyAdjustControlEnablement();

                    return string.Empty;
                }
                catch (InvalidScheduleException e)
                {
                    return e.Message;
                }
            }
            else
            {
                try
                {
                    this.schedule.MonthlyMonthRelativeBasedSetDayTime(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay, timeIndex, this.getMonthRelativeBasedTimes(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay)[timeIndex]);
                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.validate = true;
                    this.monthlyAdjustControlEnablement();

                    return string.Empty;
                }
                catch (InvalidScheduleException e)
                {
                    return e.Message;
                }
            }
        }

        private MonthRelativeCombinationViewModel monthlyMonthRelativeBasedGetCombinationViewModel(DayPosition dayPosition, DayOfWeek day)
        {
            MonthRelativeCombinationViewModel[] combinationViewModels =
                this.monthlyMonthRelativeBasedCombinationViewModels.Where(key =>
                    key.DayPosition == dayPosition &&
                    key.DayOfWeek == day).ToArray();

            int count = combinationViewModels.Length;
            if (count > 1)
            {
                throw new ArgumentOutOfRangeException("Unexpected MonthRelativeCombination count.");
            }

            if (count <= 0)
            {
                return null;
            }

            return combinationViewModels[0];
        }

        public string Error
        {
            get => string.Empty;
        }
    }
}
