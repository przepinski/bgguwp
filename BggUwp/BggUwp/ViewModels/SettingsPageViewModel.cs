﻿using BggUwp.Data;
using BggUwp.Data.Models;
using BggUwp.Messaging;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Windows.ApplicationModel;

namespace BggUwp.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        private SettingsService settingsService;

        public SettingsPageViewModel()
        {
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                settingsService = SettingsService.Instance;
                if (StorageService.RetrieveUserCredentials() != null)
                {
                    AccountStatusText = "Logged as " + StorageService.RetrieveUserCredentials().UserName;
                }
            }
        }

        string _AccountStatusText = "Sign in to BGG";
        public string AccountStatusText
        {
            get
            {
                return _AccountStatusText;
            }
            set
            {
                Set(ref _AccountStatusText, value);
            }
        }

        public void GoToAccountPartPage() => NavigationService.Navigate(typeof(Views.AccountPartPage));
        public void GoToSynchronizatonPartPage() => NavigationService.Navigate(typeof(Views.SynchronizationPartPage));
        public void GoToFeedbackPartPage() => NavigationService.Navigate(typeof(Views.FeedbackPartPage));
        public void GoToAboutPartPage() => NavigationService.Navigate(typeof(Views.AboutPartPage));
    }

    public class AccountPartViewModel : ViewModelBase
    {
        private SettingsService settingsService;

        public AccountPartViewModel()
        {
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                settingsService = SettingsService.Instance;
                if (StorageService.RetrieveUserCredentials() != null)
                {
                    Username = StorageService.RetrieveUserCredentials().UserName;
                }
            }
        }

        string _Username = default(string);
        public string Username
        {
            get
            {
                return _Username;
            }
            set
            {
                Set(ref _Username, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        string _Password = default(string);
        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                Set(ref _Password, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        private DelegateCommand _LoginCommand;
        public DelegateCommand LoginCommand
        {
            get
            {
                return _LoginCommand ??
                    (_LoginCommand = new DelegateCommand(ExecuteLoginCommand, CanExecuteLoginCommand));
            }
            set
            {
                Set(ref _LoginCommand, value);
            }
        }

        public async void ExecuteLoginCommand()
        {
            if (!CanExecuteLoginCommand())
                return;

            if (!await DataService.Instance.LoginUser(Username, Password))
            {
                return;
            }

            Messenger.Default.Send(new ProgressMessage()
            {
                State = ProgressMessage.ProgressState.Started,
                Message = "Downloading user data..."
            });

            await DataService.Instance.DownloadPlayersListFromWeb();

            Messenger.Default.Send(new ProgressMessage()
            {
                State = ProgressMessage.ProgressState.Finished,
            });
            Messenger.Default.Send<RefreshDataMessage>(new RefreshDataMessage()
            {
                RequestedRefreshScope = RefreshDataMessage.RefreshScope.All,
                RequestedRefreshType = RefreshDataMessage.RefreshType.Web
            });
            GoToSettingsPage();
        }

        private bool CanExecuteLoginCommand()
        {
            if (String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Password))
                return false;

            return true;
        }

        public void GoToSettingsPage()
        {
            NavigationService.Navigate(typeof(Views.SettingsPage));
            NavigationService.Frame.BackStack.Remove(NavigationService.Frame.BackStack.LastOrDefault());
            NavigationService.Frame.BackStack.Remove(NavigationService.Frame.BackStack.LastOrDefault());
        }
            
    }

    public class SynchronizationPartViewModel : ViewModelBase
    {
        private SettingsService settingsService;

        public SynchronizationPartViewModel()
        {
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                settingsService = SettingsService.Instance;
                ShouldUpdateOnlyOnWiFi = settingsService.UpdateDataOnlyOnWiFi;
            }
        }

        bool _ShouldUpdateOnlyOnWiFi = false;
        public bool ShouldUpdateOnlyOnWiFi
        {
            get
            {
                return _ShouldUpdateOnlyOnWiFi;
            }
            set
            {
                settingsService.UpdateDataOnlyOnWiFi = value;
                Set(ref _ShouldUpdateOnlyOnWiFi, value);
            }
        }
    }

    public class FeedbackPartViewModel : ViewModelBase
    {
        public FeedbackPartViewModel()
        {
        }

        public DelegateCommand ReportBugCommand => new DelegateCommand(async () =>
        {
            await SendEmail("[Bug] ");
        });

        public DelegateCommand SendFeedbackCommand => new DelegateCommand(async () =>
        {
            await SendEmail("[Feedback] ");
        });

        private async Task SendEmail(string subject)
        {
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage();

            var emailRecipient = new Windows.ApplicationModel.Email.EmailRecipient("bgguwp@gmail.com");
            emailMessage.To.Add(emailRecipient);
            emailMessage.Subject = subject;

            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }
    }

    public class AboutPartViewModel : ViewModelBase
    {
        public AboutPartViewModel()
        {
            VersionData = GetAppVersion();
        }

        string _VersionData = string.Empty;
        public string VersionData
        {
            get
            {
                return _VersionData;
            }
            set
            {
                Set(ref _VersionData, value);
            }
        }

        public static string GetAppVersion()
        {

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }
}