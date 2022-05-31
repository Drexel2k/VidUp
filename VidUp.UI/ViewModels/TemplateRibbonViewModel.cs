using System;
using System.Collections.Generic;
using System.ComponentModel;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class TemplateRibbonViewModel : INotifyPropertyChanged
    {
        private TemplateList templateList;

        private ObservableTemplateViewModels observableTemplateViewModels;
        private TemplateComboboxViewModel selectedTemplate;

        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private YoutubeAccount selectedYoutubeAccountForFiltering;
        private YoutubeAccount youtubeAccountForCreatingTemplates;

        //command execution doesn't need any parameter, parameter is only action to do.
        private GenericCommand parameterlessCommand;

        public event PropertyChangedEventHandler PropertyChanged;
        #region properties


        public ObservableTemplateViewModels ObservableTemplateViewModels
        {
            get
            {
                return this.observableTemplateViewModels;
            }
        }

        public TemplateComboboxViewModel SelectedTemplate
        {
            get
            {
                return this.selectedTemplate;
            }
            set
            {
                if (this.selectedTemplate != value)
                {
                    Template oldTemplate = this.selectedTemplate != null ? this.selectedTemplate.Template : null;
                    this.selectedTemplate = value;
                    EventAggregator.Instance.Publish(new SelectedTemplateChangedMessage(oldTemplate, value != null ? value.Template : null));
                    this.raisePropertyChanged("SelectedTemplate");
                }
            }
        }

        public GenericCommand ParameterlessCommand
        {
            get
            {
                return this.parameterlessCommand;
            }
        }

        #endregion properties

        public TemplateRibbonViewModel(TemplateList templateList, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels, YoutubeAccount youtubeAccountForCreatingTemplates, YoutubeAccount selectedYoutubeAccountForFiltering)
        {
            if(templateList == null)
            {
                throw new ArgumentException("TemplateList must not be null.");
            }

            if (youtubeAccountForCreatingTemplates == null)
            {
                throw new ArgumentException("selectedYoutubeAccount must not be null.");
            }

            if (observableYoutubeAccountViewModels == null)
            {
                throw new ArgumentException("ObservableYoutubeAccountViewModels must not be null.");
            }

            this.templateList = templateList;
            this.observableTemplateViewModels = new ObservableTemplateViewModels(this.templateList, true, false, false); ;
            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;

            this.SelectedTemplate = this.observableTemplateViewModels.TemplateCount > 0 ? this.observableTemplateViewModels[0] : null;

            this.selectedYoutubeAccountForFiltering = selectedYoutubeAccountForFiltering;
            this.youtubeAccountForCreatingTemplates = youtubeAccountForCreatingTemplates;

            EventAggregator.Instance.Subscribe<SelectedFilterYoutubeAccountChangedMessage>(this.selectedYoutubeAccountChanged);
            EventAggregator.Instance.Subscribe<BeforeYoutubeAccountDeleteMessage>(this.beforeYoutubeAccountDelete);

            this.parameterlessCommand = new GenericCommand(this.parameterlessCommandAction);
            EventAggregator.Instance.Subscribe<TemplateDeleteMessage>(this.deleteTemplate);
            EventAggregator.Instance.Subscribe<TemplateCopyMessage>(this.copyTemplate);
        }

        private void deleteTemplate(TemplateDeleteMessage templateDeleteMessage)
        {
            TemplateComboboxViewModel viewModel = this.observableTemplateViewModels.GetFirstViewModel(vm => vm.Template != templateDeleteMessage.Template && vm.Visible == true);
            this.SelectedTemplate = viewModel;

            this.templateList.Delete(templateDeleteMessage.Template);

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeTemplateList();
        }

        private async void copyTemplate(TemplateCopyMessage templateCopyMessage)
        {
            var view = new CopyTemplateControl
            {
                DataContext = new CopyTemplateViewModel(templateCopyMessage.Template.Name, Settings.Instance.TemplateImageFolder)
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                CopyTemplateViewModel data = (CopyTemplateViewModel)view.DataContext;
                Template template = new Template(templateCopyMessage.Template, data.Name, this.templateList);
                this.AddTemplate(template);
            }
        }

        private void beforeYoutubeAccountDelete(BeforeYoutubeAccountDeleteMessage beforeYoutubeAccountDeleteMessage)
        {
            List<Template> templatesToRemove = this.templateList.FindAll(template => template.YoutubeAccount == beforeYoutubeAccountDeleteMessage.AccountToBeDeleted);

            //Needs to set before deleting the ViewModel in ObservableTemplateViewModels, otherwise the RaiseNotifyCollectionChanged
            //will set the SelectedTemplate to null which causes problems if there are templates left
            TemplateComboboxViewModel viewModel = this.observableTemplateViewModels.GetFirstViewModel(vm => !templatesToRemove.Contains(vm.Template) && vm.Visible == true);
            this.SelectedTemplate = viewModel;

            foreach (Template template in templatesToRemove)
            {
                this.templateList.Delete(template);
            }
        }

        private void selectedYoutubeAccountChanged(SelectedFilterYoutubeAccountChangedMessage selectedYoutubeAccountChangedMessage)
        {
            if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount == null)
            {
                throw new ArgumentException("Changed Youtube account must not be null.");
            }

            this.selectedYoutubeAccountForFiltering = selectedYoutubeAccountChangedMessage.NewYoutubeAccount;
            this.youtubeAccountForCreatingTemplates = selectedYoutubeAccountChangedMessage.NewYoutubeAccount;

            if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.IsDummy)
            {
                if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.Name == "All")
                {
                    this.youtubeAccountForCreatingTemplates = selectedYoutubeAccountChangedMessage.FirstNotAllYoutubeAccount;

                    if (this.observableTemplateViewModels.TemplateCount >= 1)
                    {
                        TemplateComboboxViewModel viewModel = this.observableTemplateViewModels[0];
                        this.SelectedTemplate = viewModel;
                    }
                    else
                    {
                        this.SelectedTemplate = null;
                    }
                }
            }
            else
            {
                TemplateComboboxViewModel viewModel = this.observableTemplateViewModels.GetFirstViewModel(vm => vm.Template.YoutubeAccount == selectedYoutubeAccountChangedMessage.NewYoutubeAccount);
                this.SelectedTemplate = viewModel;
            }
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

        private async void openNewTemplateDialogAsync()
        {
            var view = new NewTemplateControl
            {
                DataContext = new NewTemplateViewModel(Settings.Instance.TemplateImageFolder, this.observableYoutubeAccountViewModels, this.youtubeAccountForCreatingTemplates)
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                NewTemplateViewModel data = (NewTemplateViewModel) view.DataContext;
                Template template = new Template(data.Name, data.ImageFilePath, data.TemplateMode, data.RootFolderPath, data.PartOfFileName, this.templateList, data.SelectedYouTubeAccount.YoutubeAccount);
                this.AddTemplate(template);

            }
        }

        private async void displayScheduleAsync()
        {
            var view = new DisplayScheduleControl
            {
                DataContext = new DisplayScheduleControlViewModel(this.selectedYoutubeAccountForFiltering.Name=="All" ?
                    this.templateList : this.templateList.FindAll(template => template.YoutubeAccount == this.selectedYoutubeAccountForFiltering))
            };

            await DialogHost.Show(view, "RootDialog");
        }

        private void parameterlessCommandAction(object target)
        {
            switch (target)
            {
                case "newtemplate":
                    this.openNewTemplateDialogAsync();
                    break;
                case "scheduleoverview":
                    this.displayScheduleAsync();
                    break;
                default:
                    throw new InvalidOperationException("Invalid parameter for parameterlessCommandAction.");
                    break;
            }
        }

        //exposed for testing
        public void AddTemplate(Template template)
        {
            this.templateList.AddTemplate(template);

            JsonSerializationContent.JsonSerializer.SerializeTemplateList();

            this.SelectedTemplate = this.observableTemplateViewModels.GetViewModel(template);
        }
    }
}
