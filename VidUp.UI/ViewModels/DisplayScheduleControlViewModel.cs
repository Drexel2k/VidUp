using Drexel.VidUp.Business;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Drexel.VidUp.UI.ViewModels
{
    public  class DisplayScheduleControlViewModel
    {
        private DateTime currentMonth;
        private IEnumerable<Template> templates;
        private List<DisplayScheduleControlWeekViewModel> displayScheduleControlWeekViewModels;
        private List<DisplayScheduleColorTemplateNameViewModel> displayScheduleColorTemplateNameViewModels;

        private Random random = new Random();

        //distinguishable colors
        private Color[] colors = new Color[] {
             Color.FromRgb(230, 25, 75),
             Color.FromRgb(60, 180, 75),
             Color.FromRgb(255, 225, 25),
             Color.FromRgb(0, 130, 200),
             Color.FromRgb(245, 130, 48),
             Color.FromRgb(145, 30, 180),
             Color.FromRgb(70, 240, 240),
             Color.FromRgb(240, 50, 230),
             Color.FromRgb(210, 245, 60),
             Color.FromRgb(250, 190, 212),
             Color.FromRgb(0, 128, 128),
             Color.FromRgb(220, 190, 255),
             Color.FromRgb(170, 110, 40),
             Color.FromRgb(255, 250, 200),
             Color.FromRgb(128, 0, 0),
             Color.FromRgb(170, 255, 195),
             Color.FromRgb(128, 128, 0),
             Color.FromRgb(255, 215, 180),
             Color.FromRgb(0, 0, 128),
             Color.FromRgb(128, 128, 128),
             Color.FromRgb(255, 255, 255),
             Color.FromRgb(0, 0, 0)
        };

        public List<DisplayScheduleControlWeekViewModel> DisplayScheduleControlWeekViewModels
        {
            get { return this.displayScheduleControlWeekViewModels; }
        }

        public List<DisplayScheduleColorTemplateNameViewModel> DisplayScheduleColorTemplateNameViewModels
        {
            get { return this.displayScheduleColorTemplateNameViewModels; }
        }

        public string MonthInfo
        {
            get => currentMonth.ToString("MMMM yyyy");
        }

        public DisplayScheduleControlViewModel(IEnumerable<Template> templates)
        {
            DateTime now = DateTime.Now;
            this.currentMonth = new DateTime(now.Year, now.Month, 1);

            this.templates = templates;

            this.displayScheduleControlWeekViewModels = new List<DisplayScheduleControlWeekViewModel>();
            this.displayScheduleColorTemplateNameViewModels = new List<DisplayScheduleColorTemplateNameViewModel>();
            
            DisplayScheduleControlDayViewModel[] dayViewModels = new DisplayScheduleControlDayViewModel[DateTime.DaysInMonth(this.currentMonth.Year, this.currentMonth.Month)];

            bool dateadded = false;
            int colorIndex = 0;
            foreach(Template template in this.templates)
            {
                if(template.UsePublishAtSchedule && template.PublishAtSchedule != null)
                {
                    //if more templates with than distinguishable colors exist.
                    Color randomColor = Color.FromRgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));

                    //Otherwise schedule with 0:00 release would skip release on first day.
                    DateTime nextDate = this.currentMonth.AddMinutes(-1);
                    bool next = true;
                    while (next)
                    {
                        nextDate = template.PublishAtSchedule.GetNextDateTime(nextDate, true);
                        if (nextDate.Month == this.currentMonth.Month && nextDate.Year == this.currentMonth.Year)
                        {
                            dateadded = true;

                            int dayIndex = nextDate.Day - 1;
                            if (dayViewModels[dayIndex] == null)
                            {
                                dayViewModels[dayIndex] = new DisplayScheduleControlDayViewModel();
                                dayViewModels[dayIndex].Day = nextDate.Day;
                                dayViewModels[dayIndex].DisplayScheduleColorQuarterHourViewModels = new List<DisplayScheduleColorQuarterHourViewModel>();
                            }

                            DisplayScheduleColorQuarterHourViewModel displayScheduleColorQuarterHourViewModel = new DisplayScheduleColorQuarterHourViewModel();
                            displayScheduleColorQuarterHourViewModel.QuarterHour = nextDate.TimeOfDay;

                            if (colorIndex < this.colors.Length)
                            {
                                displayScheduleColorQuarterHourViewModel.Color = this.colors[colorIndex];
                            }
                            else
                            {
                                displayScheduleColorQuarterHourViewModel.Color = randomColor;
                            }

                            dayViewModels[dayIndex].DisplayScheduleColorQuarterHourViewModels.Add(displayScheduleColorQuarterHourViewModel);

                            if(template.PublishAtSchedule.ScheduleFrequency == ScheduleFrequency.SpecificDate)
                            {
                                next = false;
                            }
                        }
                        else
                        {
                            next = false;
                        }
                    }

                    if(dateadded)
                    {
                        DisplayScheduleColorTemplateNameViewModel displayScheduleColorTemplateNameViewModel = new DisplayScheduleColorTemplateNameViewModel();
                        displayScheduleColorTemplateNameViewModel.TemplateName = template.Name;
                        if (colorIndex < this.colors.Length)
                        {
                            displayScheduleColorTemplateNameViewModel.Color = this.colors[colorIndex];
                        }
                        else
                        {
                            displayScheduleColorTemplateNameViewModel.Color = randomColor;
                        }

                        this.displayScheduleColorTemplateNameViewModels.Add(displayScheduleColorTemplateNameViewModel);
                        colorIndex++;
                        dateadded = false;
                    }
                }
            }

            var daysInMonth = DateTime.DaysInMonth(this.currentMonth.Year, this.currentMonth.Month);


            int currentWeekDay = (int)this.currentMonth.DayOfWeek;
            currentWeekDay = currentWeekDay - 1;
            if (currentWeekDay < 0)
            {
                currentWeekDay = 6;
            }

            bool addnewWeek = true;
            DisplayScheduleControlWeekViewModel currentDisplayScheduleControlWeekViewModel = null;
            for (int dayofMontIndex = 0; dayofMontIndex < daysInMonth; dayofMontIndex++)
            {
                if (addnewWeek)
                {
                    currentDisplayScheduleControlWeekViewModel = new DisplayScheduleControlWeekViewModel();
                    currentDisplayScheduleControlWeekViewModel.DisplayScheduleControlDayViewModels = new List<DisplayScheduleControlDayViewModel>();
                    this.displayScheduleControlWeekViewModels.Add(currentDisplayScheduleControlWeekViewModel);

                    for(int weekdayIndex = 0; weekdayIndex < currentWeekDay; weekdayIndex++)
                    {
                        DisplayScheduleControlDayViewModel displayScheduleControlDayViewModelDummy = new DisplayScheduleControlDayViewModel();
                        displayScheduleControlDayViewModelDummy.Day = -1;
                        currentDisplayScheduleControlWeekViewModel.DisplayScheduleControlDayViewModels.Add(displayScheduleControlDayViewModelDummy);
                    }

                    addnewWeek = false;
                }

                
                DisplayScheduleControlDayViewModel displayScheduleControlDayViewModel = dayViewModels[dayofMontIndex];
                if(displayScheduleControlDayViewModel == null)
                {
                    displayScheduleControlDayViewModel = new DisplayScheduleControlDayViewModel();
                    displayScheduleControlDayViewModel.Day = dayofMontIndex + 1;
                    displayScheduleControlDayViewModel.DisplayScheduleColorQuarterHourViewModels = new List<DisplayScheduleColorQuarterHourViewModel>();
                    DisplayScheduleColorQuarterHourViewModel displayScheduleColorQuarterHourViewModel = new DisplayScheduleColorQuarterHourViewModel();
                    displayScheduleColorQuarterHourViewModel.Empty = true;
                    displayScheduleControlDayViewModel.DisplayScheduleColorQuarterHourViewModels.Add(displayScheduleColorQuarterHourViewModel);
                }
                else
                {
                    displayScheduleControlDayViewModel.DisplayScheduleColorQuarterHourViewModels.Sort((first, second) =>
                        {
                            if (first.QuarterHour < second.QuarterHour)
                            {
                                return -1;
                            }

                            if (first.QuarterHour > second.QuarterHour)
                            {
                                return 1;
                            }

                            return 0;
                        }
                    );
                }

                currentDisplayScheduleControlWeekViewModel.DisplayScheduleControlDayViewModels.Add(displayScheduleControlDayViewModel);

                if (currentWeekDay == 6)
                {
                    addnewWeek = true;
                    currentWeekDay = 0;
                }
                else
                {
                    currentWeekDay++;
                }
            }

            if (currentWeekDay > 0)
            {
                for (int weekdayIndex = currentWeekDay; weekdayIndex <= 6; weekdayIndex++)
                {
                    DisplayScheduleControlDayViewModel displayScheduleControlDayViewModelDummy = new DisplayScheduleControlDayViewModel();
                    displayScheduleControlDayViewModelDummy.Day = -1;
                    currentDisplayScheduleControlWeekViewModel.DisplayScheduleControlDayViewModels.Add(displayScheduleControlDayViewModelDummy);
                }
            }
        }
    }
}