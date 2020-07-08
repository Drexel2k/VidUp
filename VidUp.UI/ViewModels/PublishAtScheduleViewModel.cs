using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms.VisualStyles;
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


        private Dictionary<int, QuarterHourViewModel[]> dailyDayTimesViewModels;

        private bool[] weeklyDays;
        private Dictionary<DayOfWeek, QuarterHourViewModel[]> weeklyDayTimesViewModels;

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

        public bool DailyDay1Time1Enabled
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

        public bool DailyDay1Time2Enabled
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

        public bool DailyDay1Time3Enabled
        {
            get
            {
                if (!this.schedule.DailyHasAdvancedSchedule)
                {
                    return false;
                }

                if (this.schedule.DailyDayTimes[0][1] != null)
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

                if (this.schedule.DailyDayTimes.ContainsKey(1) && this.schedule.DailyDayTimes[1][0] != null)
                {
                    return true;
                }

                return false;
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

                if (this.schedule.DailyDayTimes.ContainsKey(1) && this.schedule.DailyDayTimes[1][1] != null && this.schedule.DailyDayTimes[1][0] != null)
                {
                    return true;
                }

                return false;
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

                if (this.schedule.DailyDayTimes.ContainsKey(1) && this.schedule.DailyDayTimes[1][0] != null)
                {
                    return true;
                }

                return false;
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

                if (this.schedule.DailyDayTimes.ContainsKey(2) && this.schedule.DailyDayTimes[2][0] != null)
                {
                    return true;
                }

                return false;
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

                if (this.schedule.DailyDayTimes.ContainsKey(2) && this.schedule.DailyDayTimes[2][1] != null && this.schedule.DailyDayTimes[2][0] != null)
                {
                    return true;
                }

                return false;
            }
        }

        public QuarterHourViewModel DailyDay1Time1
        {
            get
            {
                return this.dailyDayTimesViewModels[0][0];
            }
            set
            {
                this.dailyDayTimesViewModels[0][0] = value;
            }
        }

        public QuarterHourViewModel DailyDay1Time2
        {
            get
            {
                return this.dailyDayTimesViewModels[0][1];
            }
            set
            {
                this.dailyDayTimesViewModels[0][1] = value;
            }
        }

        public QuarterHourViewModel DailyDay1Time3
        {
            get
            {
                return this.dailyDayTimesViewModels[0][2];
            }
            set
            {
                this.dailyDayTimesViewModels[0][2] = value;
            }
        }

        public QuarterHourViewModel DailyDay2Time1
        {
            get
            {
                return this.dailyDayTimesViewModels[1][0];
            }
            set
            {
                this.dailyDayTimesViewModels[1][0] = value;
            }
        }

        public QuarterHourViewModel DailyDay2Time2
        {
            get
            {
                return this.dailyDayTimesViewModels[1][1];
            }
            set
            {
                this.dailyDayTimesViewModels[1][1] = value;
            }
        }

        public QuarterHourViewModel DailyDay2Time3
        {
            get
            {
                return this.dailyDayTimesViewModels[1][2];
            }
            set
            {
                this.dailyDayTimesViewModels[1][2] = value;
            }
        }

        public QuarterHourViewModel DailyDay3Time1
        {
            get
            {
                return this.dailyDayTimesViewModels[2][0];
            }
            set
            {
                this.dailyDayTimesViewModels[2][0] = value;
            }
        }

        public QuarterHourViewModel DailyDay3Time2
        {
            get
            {
                return this.dailyDayTimesViewModels[2][1];
            }
            set
            {
                this.dailyDayTimesViewModels[2][1] = value;
            }
        }

        public QuarterHourViewModel DailyDay3Time3
        {
            get
            {
                return this.dailyDayTimesViewModels[2][2];
            }
            set
            {
                this.dailyDayTimesViewModels[2][2] = value;
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
                this.raisePropertyChanged("WeeklyHasAdvancedSchedule");
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

                if (!this.schedule.WeeklyDays[0])
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

                if (!this.schedule.WeeklyDays[0])
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

                if (!this.schedule.WeeklyDays[0])
                {
                    return false;
                }

                if (this.schedule.WeeklyDayTimes[DayOfWeek.Monday][1] == null)
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

                if (!this.schedule.WeeklyDays[1])
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

                if (!this.schedule.WeeklyDays[1])
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

                if (!this.schedule.WeeklyDays[1])
                {
                    return false;
                }

                if (this.schedule.WeeklyDayTimes[DayOfWeek.Tuesday][1] == null)
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

                if (!this.schedule.WeeklyDays[2])
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

                if (!this.schedule.WeeklyDays[2])
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

                if (!this.schedule.WeeklyDays[2])
                {
                    return false;
                }

                if (this.schedule.WeeklyDayTimes[DayOfWeek.Wednesday][1] == null)
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

                if (!this.schedule.WeeklyDays[3])
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

                if (!this.schedule.WeeklyDays[3])
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

                if (!this.schedule.WeeklyDays[3])
                {
                    return false;
                }

                if (this.schedule.WeeklyDayTimes[DayOfWeek.Thursday][1] == null)
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

                if (!this.schedule.WeeklyDays[4])
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

                if (!this.schedule.WeeklyDays[4])
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

                if (!this.schedule.WeeklyDays[4])
                {
                    return false;
                }

                if (this.schedule.WeeklyDayTimes[DayOfWeek.Friday][1] == null)
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

                if (!this.schedule.WeeklyDays[5])
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

                if (!this.schedule.WeeklyDays[5])
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

                if (!this.schedule.WeeklyDays[5])
                {
                    return false;
                }

                if (this.schedule.WeeklyDayTimes[DayOfWeek.Saturday][1] == null)
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

                if (!this.schedule.WeeklyDays[6])
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

                if (!this.schedule.WeeklyDays[6])
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

                if (!this.schedule.WeeklyDays[6])
                {
                    return false;
                }

                if (this.schedule.WeeklyDayTimes[DayOfWeek.Sunday][1] == null)
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
                return this.weeklyDayTimesViewModels[DayOfWeek.Monday][0];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Monday][0] = value;
            }
        }

        public QuarterHourViewModel WeeklyMondayTime2
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Monday][1];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Monday][1] = value;
            }
        }

        public QuarterHourViewModel WeeklyMondayTime3
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Monday][2];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Monday][2] = value;
            }
        }

        public QuarterHourViewModel WeeklyTuesdayTime1
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Tuesday][0];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Tuesday][0] = value;
            }
        }

        public QuarterHourViewModel WeeklyTuesdayTime2
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Tuesday][1];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Tuesday][1] = value;
            }
        }

        public QuarterHourViewModel WeeklyTuesdayTime3
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Tuesday][2];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Tuesday][2] = value;
            }
        }

        public QuarterHourViewModel WeeklyWednesdayTime1
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Wednesday][0];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Wednesday][0] = value;
            }
        }

        public QuarterHourViewModel WeeklyWednesdayTime2
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Wednesday][1];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Wednesday][1] = value;
            }
        }

        public QuarterHourViewModel WeeklyWednesdayTime3
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Wednesday][2];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Wednesday][2] = value;
            }
        }

        public QuarterHourViewModel WeeklyThursdayTime1
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Thursday][0];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Thursday][0] = value;
            }
        }

        public QuarterHourViewModel WeeklyThursdayTime2
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Thursday][1];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Thursday][1] = value;
            }
        }

        public QuarterHourViewModel WeeklyThursdayTime3
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Thursday][2];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Thursday][2] = value;
            }
        }

        public QuarterHourViewModel WeeklyFridayTime1
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Friday][0];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Friday][0] = value;
            }
        }

        public QuarterHourViewModel WeeklyFridayTime2
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Friday][1];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Friday][1] = value;
            }
        }

        public QuarterHourViewModel WeeklyFridayTime3
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Friday][2];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Friday][2] = value;
            }
        }

        public QuarterHourViewModel WeeklySaturdayTime1
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Saturday][0];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Saturday][0] = value;
            }
        }

        public QuarterHourViewModel WeeklySaturdayTime2
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Saturday][1];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Saturday][1] = value;
            }
        }

        public QuarterHourViewModel WeeklySaturdayTime3
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Saturday][2];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Saturday][2] = value;
            }
        }

        public QuarterHourViewModel WeeklySundayTime1
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Sunday][0];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Sunday][0] = value;
            }
        }

        public QuarterHourViewModel WeeklySundayTime2
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Sunday][1];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Sunday][1] = value;
            }
        }

        public QuarterHourViewModel WeeklySundayTime3
        {
            get
            {
                return this.weeklyDayTimesViewModels[DayOfWeek.Sunday][2];
            }
            set
            {
                this.weeklyDayTimesViewModels[DayOfWeek.Sunday][2] = value;
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
                this.raisePropertyChanged("MonthlyHasAdvancedSchedule");
                this.monthlyAdjustControlEnablement();
            }
        }

        public bool MonthlyTime1Enabled
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    return this.schedule.MonthlyHasAdvancedSchedule && this.schedule.MonthlyMonthDateBasedDates[this.monthlyMonthDateBasedDay.Day - 1];
                }
                else
                {
                    return this.schedule.MonthlyHasAdvancedSchedule && this.schedule.MonthlyMonthRelativeBasedCombinations.Exists(
                        mrc => mrc.DayPosition == this.monthlyMonthRelativeBasedDayPosition && mrc.DayOfWeek == this.monthlyMonthRelativeBasedDay);
                }
            }
        }

        public bool MonthlyTime2Enabled
        {
            get
            {
                if (this.schedule.MonthlyDateBased)
                {
                    return this.schedule.MonthlyHasAdvancedSchedule && this.schedule.MonthlyMonthDateBasedDates[this.monthlyMonthDateBasedDay.Day - 1];
                }
                else
                {
                    return this.schedule.MonthlyHasAdvancedSchedule && this.schedule.MonthlyMonthRelativeBasedCombinations.Exists(
                        mrc => mrc.DayPosition == this.monthlyMonthRelativeBasedDayPosition && mrc.DayOfWeek == this.monthlyMonthRelativeBasedDay);
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
                    if (!this.schedule.MonthlyMonthDateBasedDayTimes.ContainsKey(this.monthlyMonthDateBasedDay.Day - 1))
                    {
                        return false;
                    }

                    if (this.schedule.MonthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][1] != null)
                    {
                        return true;
                    }

                    return false;
                }
                else
                {
                    TimeSpan?[] timeSpans = this.getMonthRelativeBasedTimesFromSchedule(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                    if (timeSpans != null)
                    {
                        if (timeSpans[1] != null)
                        {
                            return true;
                        }
                    }

                    return false;
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
                    TimeSpan?[] timeSpans = this.getMonthRelativeBasedTimesFromViewModel(
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
                        this.getMonthRelativeBasedTimesFromViewModel(this.monthlyMonthRelativeBasedDayPosition,
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
                    TimeSpan?[] timeSpans = this.getMonthRelativeBasedTimesFromViewModel(
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
                        this.getMonthRelativeBasedTimesFromViewModel(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
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
                    TimeSpan?[] timeSpans = this.getMonthRelativeBasedTimesFromViewModel(
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
                        this.getMonthRelativeBasedTimesFromViewModel(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                    timeSpans[2] = value.QuarterHour;
                }
            }
        }

        private TimeSpan?[] getMonthRelativeBasedTimesFromSchedule(DayPosition dayPosition, DayOfWeek day)
        {
            MonthRelativeCombination[] combinations =
                this.schedule.MonthlyMonthRelativeBasedDayTimes.Keys.Where(key =>
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

            return this.schedule.MonthlyMonthRelativeBasedDayTimes[combinations[0]];
        }

        private TimeSpan?[] getMonthRelativeBasedTimesFromViewModel(DayPosition dayPosition, DayOfWeek day)
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

        private MonthRelativeCombination getMonthRelativeBasedCombinationFromViewModel(DayPosition dayPosition, DayOfWeek day)
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

        private MonthRelativeCombination getMonthRelativeBasedCombinationFromViewModelDayTimes(DayPosition dayPosition, DayOfWeek day)
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

        private MonthRelativeCombination getMonthRelativeBasedCombinationFromScheduleDayTimes(DayPosition dayPosition, DayOfWeek day)
        {
            MonthRelativeCombination[] combinations =
                this.schedule.MonthlyMonthRelativeBasedDayTimes.Keys.Where(key =>
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

        private MonthRelativeCombinationViewModel getMonthRelativeBasedCombinationViewModel(DayPosition dayPosition, DayOfWeek day)
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

        private MonthRelativeCombination getMonthRelativeBasedCombinationFromSchedule(DayPosition dayPosition, DayOfWeek day)
        {
            MonthRelativeCombination[] combinations =
                this.schedule.MonthlyMonthRelativeBasedCombinations.Where(key =>
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
                            MonthRelativeCombination monthRelativeCombination = this.getMonthRelativeBasedCombinationFromViewModelDayTimes(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                            if (monthRelativeCombination == null)
                            {
                                monthRelativeCombination = new MonthRelativeCombination(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                            }

                            this.monthlyMonthRelativeBasedCombinations.Add(monthRelativeCombination);
                            MonthRelativeCombinationViewModel monthRelativeCombinationViewModel = new MonthRelativeCombinationViewModel(monthRelativeCombination);
                            this.monthlyMonthRelativeBasedCombinationViewModels.Add(monthRelativeCombinationViewModel);
                            this.monthlyMonthRelativeBasedCombination = monthRelativeCombinationViewModel;
                        }
                    }
                    else
                    {
                        MonthRelativeCombination monthRelativeCombination = this.getMonthRelativeBasedCombinationFromViewModel(
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
                }
                else
                {
                    this.schedule.MonthlyDateBased = false;
                }

                this.validate = false;
                this.raisePropertyChanged("MonthlyMonthDateBasedIndex");
                this.raisePropertyChanged("MonthlyMonthDateBasedVisible");
                this.raisePropertyChanged("MonthlyMonthRelativeBasedVisible");
                this.raisePropertyChanged("MonthlyActive");
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

            this.initializeDailyViewModels();
            this.initializeWeeklyViewModels();
            this.initializeMonthlyMonthDateBasedDayViewModels();
            this.initializeMonthlyMonthRelativeViewModels();
        }

        private void initializeDailyViewModels()
        {
            this.dailyDayTimesViewModels = new Dictionary<int, QuarterHourViewModel[]>();
            this.dailyDayTimesViewModels.Add(0, new QuarterHourViewModel[3]);
            this.dailyDayTimesViewModels.Add(1, new QuarterHourViewModel[3]);
            this.dailyDayTimesViewModels.Add(2, new QuarterHourViewModel[3]);

            for (int day = 0; day < 3; day++)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    if (day == 0 && slot == 0)
                    {
                        this.dailyDayTimesViewModels[day][slot] =
                            this.quarterHourViewModels.GetQuarterHourViewModel(
                                this.schedule.DailyDayTimes.ContainsKey(day) ? this.schedule.DailyDayTimes[day][slot] : null);
                    }
                    else
                    {
                        this.dailyDayTimesViewModels[day][slot] =
                            this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(
                                this.schedule.DailyDayTimes.ContainsKey(day) ? this.schedule.DailyDayTimes[day][slot] : null);
                    }
                }
            }
        }

        private void initializeWeeklyViewModels()
        {
            this.weeklyDays = new bool[7];
            for (int day = 0; day < 7; day++)
            {
                this.weeklyDays[day] = this.schedule.WeeklyDays[day];
            }

            this.weeklyDayTimesViewModels = new Dictionary<DayOfWeek, QuarterHourViewModel[]>();
            this.weeklyDayTimesViewModels[DayOfWeek.Monday] = new QuarterHourViewModel[3];
            this.weeklyDayTimesViewModels[DayOfWeek.Tuesday] = new QuarterHourViewModel[3];
            this.weeklyDayTimesViewModels[DayOfWeek.Wednesday] = new QuarterHourViewModel[3];
            this.weeklyDayTimesViewModels[DayOfWeek.Thursday] = new QuarterHourViewModel[3];
            this.weeklyDayTimesViewModels[DayOfWeek.Friday] = new QuarterHourViewModel[3];
            this.weeklyDayTimesViewModels[DayOfWeek.Saturday] = new QuarterHourViewModel[3];
            this.weeklyDayTimesViewModels[DayOfWeek.Sunday] = new QuarterHourViewModel[3];
            Array days = Enum.GetValues(typeof(DayOfWeek));
            foreach (DayOfWeek day in days)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    if (slot == 0)
                    {
                        this.weeklyDayTimesViewModels[day][slot] =
                            this.quarterHourViewModels.GetQuarterHourViewModel(
                                this.schedule.WeeklyDayTimes.ContainsKey(day) ? this.schedule.WeeklyDayTimes[day][slot] : null);
                    }
                    else
                    {
                        this.weeklyDayTimesViewModels[day][slot] =
                            this.quarterHourViewModelsEmptyStartValue.GetQuarterHourViewModel(
                                this.schedule.WeeklyDayTimes.ContainsKey(day) ? this.schedule.WeeklyDayTimes[day][slot] : null);
                    }
                }
            }
        }

        private void initializeMonthlyMonthDateBasedDayViewModels()
        {
            this.monthlyMonthDateBasedDates = new bool[31];
            this.monthlyMonthDateBasedDayViewModels = new List<MonthDayViewModel>(31);
            this.monthlyMonthDateBasedDayTimes = new Dictionary<int, TimeSpan?[]>();
            for (int day = 0; day <= 30; day++)
            {
                this.monthlyMonthDateBasedDates[day] = this.schedule.MonthlyMonthDateBasedDates[day];

                MonthDayViewModel monthDayViewModel = new MonthDayViewModel(day + 1);
                if (this.monthlyMonthDateBasedDates[day])
                {
                    monthDayViewModel.Active = true;
                    this.monthlyMonthDateBasedDayTimes.Add(day, this.schedule.MonthlyMonthDateBasedDayTimes[day].ToArray());
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

            foreach (KeyValuePair<MonthRelativeCombination, TimeSpan?[]> scheduleMonthlyMonthRelativeBasedDayTime in this.schedule.MonthlyMonthRelativeBasedDayTimes)
            {
                MonthRelativeCombination monthRelativeCombination =
                    this.getMonthRelativeBasedCombinationFromViewModel(scheduleMonthlyMonthRelativeBasedDayTime.Key.DayPosition, scheduleMonthlyMonthRelativeBasedDayTime.Key.DayOfWeek);

                if (monthRelativeCombination == null)
                {
                    monthRelativeCombination = new MonthRelativeCombination(scheduleMonthlyMonthRelativeBasedDayTime.Key.DayPosition, scheduleMonthlyMonthRelativeBasedDayTime.Key.DayOfWeek);
                }

                TimeSpan?[] newTimeSpans = new TimeSpan?[3];
                for (int index = 0; index < 3; index++)
                {
                    if (scheduleMonthlyMonthRelativeBasedDayTime.Value[index] != null)
                    {
                        newTimeSpans[index] = scheduleMonthlyMonthRelativeBasedDayTime.Value[index];
                    }
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
            //this.raisePropertyChanged("MonthlyMonthRelativeBasedCombinationViewModels");
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


        //todo: move integrity logic to Schedule class
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

        private string validateWeeklyActive(string propertyName)
        {
            string day1 = propertyName.Substring(6, propertyName.Length - 12);
            int dayIndex = this.getDayIndex(day1);
            if (!this.weeklyDays[dayIndex])
            {
                int count = this.schedule.WeeklyDays.Count(v => v == true);
                if (count < 2)
                {
                    return $"{day1} is the last active day and can't be disabled.";
                }
            }

            this.schedule.WeeklyDays[dayIndex] = this.weeklyDays[dayIndex];
            this.validate = false;
            this.raisePropertyChanged(propertyName);
            this.validate = true;
            this.weeklyAdjustControlEnablement();
            return string.Empty;
        }

        private string validateDailyTimes(string propertyName)
        {
            switch (propertyName)
            {
                case "DailyDay1Time1":
                    if (this.dailyDayTimesViewModels[0][0].QuarterHour >= this.schedule.DailyDayTimes[0][1])
                    {
                        return "Day 1 Time 1 must be smaller than Day 1 Time 2.";
                    }
                    else
                    {
                        this.schedule.DailyDayTimes[0][0] = this.dailyDayTimesViewModels[0][0].QuarterHour;
                        this.validate = false;
                        this.raisePropertyChanged(propertyName);
                        this.validate = true;
                        return string.Empty;
                    }
                case "DailyDay1Time2":
                    if (this.dailyDayTimesViewModels[0][1].QuarterHour == null)
                    {
                        if (this.schedule.DailyDayTimes[0][2] != null)
                        {
                            return "Day 1 Time 2 must not have a value if Day 1 Time 3 has no value.";
                        }
                    }
                    else
                    {
                        if (this.dailyDayTimesViewModels[0][1].QuarterHour <= this.schedule.DailyDayTimes[0][0] ||
                            (this.schedule.DailyDayTimes[0][2] != null &&
                             this.dailyDayTimesViewModels[0][1].QuarterHour >= this.schedule.DailyDayTimes[0][2]))
                        {
                            return "Day 1 Time 2 must be greater than Day 1 Time 1 and smaller than Day 1 Time 3.";
                        }
                    }

                    this.schedule.DailyDayTimes[0][1] = this.dailyDayTimesViewModels[0][1].QuarterHour;
                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.validate = true;
                    this.dailyAdjustControlEnablement();
                    return string.Empty;
                case "DailyDay1Time3":
                    if (this.dailyDayTimesViewModels[0][2].QuarterHour <= this.schedule.DailyDayTimes[0][1])
                    {
                        return "Day 1 Time 3 must be greater than Day 1 Time 2.";
                    }
                    else
                    {
                        this.schedule.DailyDayTimes[0][2] = this.dailyDayTimesViewModels[0][2].QuarterHour;
                        this.validate = false;
                        this.raisePropertyChanged(propertyName);
                        this.validate = true;
                        this.dailyAdjustControlEnablement();
                        return string.Empty;
                    }
                case "DailyDay2Time1":
                    if (this.dailyDayTimesViewModels[1][0].QuarterHour == null)
                    {
                        if (this.DailyDay2Time2.QuarterHour != null || this.DailyDay3Time1.QuarterHour != null)
                        {
                            return "Day 2 Time 2 or Day 3 Time 1 must not have a value if Day 2 Time 1 has no value.";
                        }

                        this.schedule.DailyDayTimes.Remove(1);
                    }
                    else
                    {
                        TimeSpan?[] timeSpans;
                        if (this.schedule.DailyDayTimes.TryGetValue(1, out timeSpans))
                        {
                            if (this.dailyDayTimesViewModels[1][0].QuarterHour >= this.schedule.DailyDayTimes[1][1])
                            {
                                return "Day 2 Time 1 must be smaller than Day 2 Time 2.";
                            }
                        }
                        else
                        {
                            timeSpans = new TimeSpan?[3];
                            this.schedule.DailyDayTimes.Add(1, timeSpans);
                        }

                        timeSpans[0] = this.dailyDayTimesViewModels[1][0].QuarterHour;
                    }

                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.validate = true;
                    this.dailyAdjustControlEnablement();
                    return string.Empty;
                case "DailyDay2Time2":
                    if (this.dailyDayTimesViewModels[1][1].QuarterHour == null)
                    {
                        if (this.schedule.DailyDayTimes[1][2] != null)
                        {
                            return "Day 2 Time 3 must not have a value if Day 2 Time 2 has no value.";
                        }
                    }
                    else
                    {
                        if (this.dailyDayTimesViewModels[1][1].QuarterHour <= this.schedule.DailyDayTimes[1][0] ||
                            (this.schedule.DailyDayTimes[1][2] != null &&
                             this.dailyDayTimesViewModels[1][1].QuarterHour >= this.schedule.DailyDayTimes[1][2]))
                        {
                            return "Day 2 Time 2 must be greater than Day 2 Time 1 and smaller than Day 2 Time 3.";
                        }
                    }

                    this.schedule.DailyDayTimes[1][1] = this.dailyDayTimesViewModels[1][1].QuarterHour;
                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.validate = true;
                    this.dailyAdjustControlEnablement();
                    return string.Empty;
                case "DailyDay2Time3":
                    if (this.dailyDayTimesViewModels[1][2].QuarterHour <= this.schedule.DailyDayTimes[1][1])
                    {
                        return "Day 2 Time 3 must be greater than Day 2 Time 2.";
                    }
                    else
                    {
                        this.schedule.DailyDayTimes[1][2] = this.dailyDayTimesViewModels[1][2].QuarterHour;
                        this.validate = false;
                        this.raisePropertyChanged(propertyName);
                        this.validate = true;
                        this.dailyAdjustControlEnablement();
                        return string.Empty;
                    }
                case "DailyDay3Time1":
                    if (this.dailyDayTimesViewModels[2][0].QuarterHour == null)
                    {
                        if (this.DailyDay3Time2.QuarterHour != null)
                        {
                            return "Day 3 Time 1 must not have a value if Day 3 Time 2 has no value.";
                        }

                        this.schedule.DailyDayTimes.Remove(2);
                    }
                    else
                    {
                        TimeSpan?[] timeSpans;
                        if (this.schedule.DailyDayTimes.TryGetValue(2, out timeSpans))
                        {
                            if (this.dailyDayTimesViewModels[2][0].QuarterHour >= this.schedule.DailyDayTimes[2][1])
                            {
                                return "Day 3 Time 1 must be smaller than Day 3 Time 2.";
                            }
                        }
                        else
                        {
                            timeSpans = new TimeSpan?[3];
                            this.schedule.DailyDayTimes.Add(2, timeSpans);
                        }

                        timeSpans[0] = this.dailyDayTimesViewModels[2][0].QuarterHour;
                    }

                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.validate = true;
                    this.dailyAdjustControlEnablement();
                    return string.Empty;
                case "DailyDay3Time2":
                    if (this.dailyDayTimesViewModels[2][1].QuarterHour == null)
                    {
                        if (this.schedule.DailyDayTimes[2][2] != null)
                        {
                            return "Day 3 Time 3 must not have a value if Day 3 Time 2 has no value.";
                        }
                    }
                    else
                    {
                        if (this.dailyDayTimesViewModels[2][1].QuarterHour <= this.schedule.DailyDayTimes[2][0] ||
                            (this.schedule.DailyDayTimes[2][2] != null &&
                             this.dailyDayTimesViewModels[2][1].QuarterHour >= this.schedule.DailyDayTimes[2][2]))
                        {
                            return "Day 3 Time 2 must be greater than Day 3 Time 1 and smaller than Day 3 Time 3.";
                        }
                    }

                    this.schedule.DailyDayTimes[2][1] = this.dailyDayTimesViewModels[2][1].QuarterHour;
                    this.validate = false;
                    this.raisePropertyChanged(propertyName);
                    this.validate = true;
                    this.dailyAdjustControlEnablement();
                    return string.Empty;
                case "DailyDay3Time3":
                    if (this.dailyDayTimesViewModels[2][2].QuarterHour <= this.schedule.DailyDayTimes[2][1])
                    {
                        return "Day 3 Time 3 must be greater than Day 3 Time 2.";
                    }
                    else
                    {
                        this.schedule.DailyDayTimes[2][2] = this.dailyDayTimesViewModels[2][2].QuarterHour;
                        this.validate = false;
                        this.raisePropertyChanged(propertyName);
                        this.validate = true;
                        this.dailyAdjustControlEnablement();
                        return string.Empty;
                    }
                default:
                    throw new ArgumentException("Unknown property to validate.");
            }
        }

        private string validateWeeklyTimes(string propertyName)
        {
            string dayString = propertyName.Substring(6, propertyName.Length - 11);
            DayOfWeek day2 = (DayOfWeek) Enum.Parse(typeof(DayOfWeek), dayString);
            int index = Convert.ToInt32(propertyName.Substring(propertyName.Length - 1, 1)) - 1;
            if (index == 0)
            {
                if (this.schedule.WeeklyDayTimes[day2][index + 1] != null &&
                    this.weeklyDayTimesViewModels[day2][index].QuarterHour >= this.schedule.WeeklyDayTimes[day2][index + 1])
                {
                    return $"{dayString} Time 1 must be smaller than {dayString} Time 2.";
                }
            }

            if (index == 1)
            {
                if (this.weeklyDayTimesViewModels[day2][index].QuarterHour == null)
                {
                    if (this.schedule.WeeklyDayTimes[day2][index + 1] != null)
                    {
                        return $"{dayString} Time 3 must not have a value if {dayString} Time 2 has no value.";
                    }
                }
                else
                {
                    if (this.weeklyDayTimesViewModels[day2][index].QuarterHour <=
                        this.schedule.WeeklyDayTimes[day2][index - 1] ||
                        (this.schedule.WeeklyDayTimes[day2][index + 1] != null &&
                         this.weeklyDayTimesViewModels[day2][index].QuarterHour >=
                         this.schedule.WeeklyDayTimes[day2][index + 1]))
                    {
                        return
                            $"{dayString} Time 2 must be greater than {dayString} Time 1 and smaller than {dayString} Time 3.";
                    }
                }
            }

            if (index == 2)
            {
                if (this.weeklyDayTimesViewModels[day2][index].QuarterHour != null &&
                    this.weeklyDayTimesViewModels[day2][index].QuarterHour <= this.schedule.WeeklyDayTimes[day2][index - 1])
                {
                    return $"{dayString} Time 3 must be greater than {dayString} Time 2.";
                }
            }

            this.schedule.WeeklyDayTimes[day2][index] = this.weeklyDayTimesViewModels[day2][index].QuarterHour;
            this.validate = false;
            this.raisePropertyChanged(propertyName);
            this.validate = true;
            this.weeklyAdjustControlEnablement();
            return string.Empty;
        }

        private string validateMonthlyActive(string propertyName)
        {
            if (this.schedule.MonthlyDateBased)
            {
                int dayIndex2 = this.monthlyMonthDateBasedDay.Day - 1;
                if (!this.monthlyMonthDateBasedDates[dayIndex2])
                {
                    int count = this.schedule.MonthlyMonthDateBasedDates.Count(dayActive =>
                        dayActive == true);
                    if (this.schedule.MonthlyMonthDateBasedDates.Count(dayActive => dayActive == true) <= 1)
                    {
                        //as MontlyActive state can be change by selection of other controls, we need to revert the change, to prevent inconsistent data.
                        this.monthlyMonthDateBasedDates[dayIndex2] = true;
                        return $"{this.monthlyMonthDateBasedDay.Day}. is the last active day and can't be disabled.";
                    }
                }

                this.schedule.MonthlyMonthDateBasedDates[dayIndex2] = this.monthlyMonthDateBasedDates[dayIndex2];

                if (this.monthlyMonthDateBasedDates[dayIndex2])
                {
                    if (!this.monthlyMonthDateBasedDayTimes.ContainsKey(this.monthlyMonthDateBasedDay.Day - 1))
                    {
                        this.monthlyMonthDateBasedDayTimes.Add(this.monthlyMonthDateBasedDay.Day - 1, new TimeSpan?[3]);
                        this.monthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][0] = new TimeSpan();
                        this.schedule.MonthlyMonthDateBasedDayTimes.Add(this.monthlyMonthDateBasedDay.Day - 1,
                            new TimeSpan?[3]);
                        this.schedule.MonthlyMonthDateBasedDayTimes[this.monthlyMonthDateBasedDay.Day - 1][0] = new TimeSpan();
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
            else
            {
                if (!this.monthlyMonthRelativeBasedCombinations.Exists(
                    combi => combi.DayPosition == this.monthlyMonthRelativeBasedDayPosition &&
                             combi.DayOfWeek == this.monthlyMonthRelativeBasedDay))
                {
                    if (this.schedule.MonthlyMonthRelativeBasedCombinations.Count <= 1)
                    {
                        //as MontlyActive state can be change by selection of other controls, we need to revert the change, to prevent inconsistent data.
                        MonthRelativeCombination monthRelativeCombination =
                            this.getMonthRelativeBasedCombinationFromViewModelDayTimes(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                        this.monthlyMonthRelativeBasedCombinations.Add(monthRelativeCombination);
                        return "Cannot remove last relative day position.";
                    }

                    MonthRelativeCombination monthRelativeCombinationFromSchedule = this.getMonthRelativeBasedCombinationFromSchedule(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                    this.schedule.MonthlyMonthRelativeBasedCombinations.Remove(monthRelativeCombinationFromSchedule);

                    MonthRelativeCombinationViewModel monthRelativeCombinationViewModel = this.getMonthRelativeBasedCombinationViewModel(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                    //to avoid to have an empty combobox selected item. change needs to be raised to view bevore item is removed.
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
                    MonthRelativeCombination monthRelativeCombinationFromViewModel = this.getMonthRelativeBasedCombinationFromViewModel(
                        this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                    MonthRelativeCombination monthRelativeCombinationForSchedule =
                        this.getMonthRelativeBasedCombinationFromScheduleDayTimes(this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                    if (monthRelativeCombinationForSchedule == null)
                    {
                        monthRelativeCombinationForSchedule = new MonthRelativeCombination(
                            this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                    }

                    //schedule.MonthlyMonthRelativeBasedCombinations can already contain the combination
                    //if this is reversion of trying to delete the last combination entry
                    if (!this.schedule.MonthlyMonthRelativeBasedCombinations.Contains(monthRelativeCombinationForSchedule))
                    {
                        this.schedule.MonthlyMonthRelativeBasedCombinations.Add(monthRelativeCombinationForSchedule);
                    }

                    if (!this.monthlyMonthRelativeBasedDayTimes.ContainsKey(monthRelativeCombinationFromViewModel))
                    {
                        if (!this.monthlyMonthRelativeBasedDayTimes.ContainsKey(monthRelativeCombinationFromViewModel))
                        {
                            this.monthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombinationFromViewModel, new TimeSpan?[3]);
                            this.monthlyMonthRelativeBasedDayTimes[monthRelativeCombinationFromViewModel][0] = new TimeSpan();

                            this.schedule.MonthlyMonthRelativeBasedDayTimes.Add(monthRelativeCombinationForSchedule, new TimeSpan?[3]);
                            this.schedule.MonthlyMonthRelativeBasedDayTimes[monthRelativeCombinationForSchedule][0] = new TimeSpan();
                        }
                    }
                }

                this.validate = false;
                this.raisePropertyChanged(propertyName);

                //this.raisePropertyChanged("MonthlyMonthRelativeBasedCombinationViewModels");
                this.raisePropertyChanged("MonthlyTime1");
                this.raisePropertyChanged("MonthlyTime2");
                this.raisePropertyChanged("MonthlyTime3");
                this.validate = true;
                this.monthlyAdjustControlEnablement();
                return string.Empty;
            }
        }

        private string validateMonthlyTimes(string propertyName)
        {
            int index2 = Convert.ToInt32(propertyName.Substring(11, 1)) - 1;
            if (this.schedule.MonthlyDateBased)
            {
                int dayIndex3 = this.monthlyMonthDateBasedDay.Day - 1;
                if (index2 == 0)
                {
                    if (this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2 + 1] != null &&
                        this.monthlyMonthDateBasedDayTimes[dayIndex3][index2] >=
                        this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2 + 1])
                    {
                        return $"Monthly Time 1 must be smaller than monthly Time 2.";
                    }
                }

                if (index2 == 1)
                {
                    if (this.monthlyMonthDateBasedDayTimes[dayIndex3][index2] == null)
                    {
                        if (this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2 + 1] != null)
                        {
                            return
                                $"Monthly Time 3 must not have a value if monthly Time 2 has no value.";
                        }
                    }
                    else
                    {
                        if (this.monthlyMonthDateBasedDayTimes[dayIndex3][index2] <=
                            this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2 - 1] ||
                            (this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2 + 1] != null &&
                             this.monthlyMonthDateBasedDayTimes[dayIndex3][index2] >=
                             this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2 + 1]))
                        {
                            return
                                $"Monthly Time 2 must be greater than monthly Time 1 and smaller than monthly Time 3.";
                        }
                    }
                }

                if (index2 == 2)
                {
                    if (this.monthlyMonthDateBasedDayTimes[dayIndex3][index2] != null &&
                        this.monthlyMonthDateBasedDayTimes[dayIndex3][index2] <=
                        this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2 - 1])
                    {
                        return $"Monthly Time 3 must be greater than monthly Time 2.";
                    }
                }

                this.schedule.MonthlyMonthDateBasedDayTimes[dayIndex3][index2] =
                    this.monthlyMonthDateBasedDayTimes[dayIndex3][index2];
                this.validate = false;
                this.raisePropertyChanged(propertyName);
                this.validate = true;
                this.monthlyAdjustControlEnablement();

                return string.Empty;
            }
            else
            {
                TimeSpan?[] timeSpansViewModel = this.getMonthRelativeBasedTimesFromViewModel(
                    this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);
                TimeSpan?[] timeSpansSchedule = this.getMonthRelativeBasedTimesFromSchedule(
                    this.monthlyMonthRelativeBasedDayPosition, this.monthlyMonthRelativeBasedDay);

                if (index2 == 0)
                {
                    if (timeSpansSchedule[index2 + 1] != null &&
                        timeSpansViewModel[index2] >= timeSpansSchedule[index2 + 1])
                    {
                        return $"Monthly Time 1 must be smaller than monthly Time 2.";
                    }
                }

                if (index2 == 1)
                {
                    if (timeSpansViewModel[index2] == null)
                    {
                        if (timeSpansSchedule[index2 + 1] != null)
                        {
                            return
                                $"Monthly Time 3 must not have a value if monthly Time 2 has no value.";
                        }
                    }
                    else
                    {
                        if (timeSpansViewModel[index2] <= timeSpansSchedule[index2 - 1] ||
                            (timeSpansSchedule[index2 + 1] != null && timeSpansViewModel[index2] >= timeSpansSchedule[index2 + 1]))
                        {
                            return
                                $"Monthly Time 2 must be greater than monthly Time 1 and smaller than monthly Time 3.";
                        }
                    }
                }

                if (index2 == 2)
                {
                    if (timeSpansViewModel[index2] != null &&
                        timeSpansViewModel[index2] <= timeSpansSchedule[index2 - 1])
                    {
                        return $"Monthly Time 3 must be greater than monthly Time 2.";
                    }
                }

                timeSpansSchedule[index2] = timeSpansViewModel[index2];
                this.validate = false;
                this.raisePropertyChanged(propertyName);
                this.validate = true;
                this.monthlyAdjustControlEnablement();

                return string.Empty;
            }
        }

        private int getDayIndex(string day)
        {
            switch (day)
            {
                case "Monday":
                    return 0;
                case "Tuesday":
                    return 1;
                case "Wednesday":
                    return 2;
                case "Thursday":
                    return 3;
                case "Friday":
                    return 4;
                case "Saturday":
                    return 5;
                case "Sunday":
                    return 6;
                default:
                    throw new ArgumentException("Day not found.");
            }
        }

        public string Error
        {
            get => string.Empty;
        }
    }
}
