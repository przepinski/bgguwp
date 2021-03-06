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
using Windows.UI.Xaml.Controls;

namespace BggUwp.ViewModels
{
    public class LogPlayViewModel : ViewModelBase
    {
        public LogPlayViewModel() { }
        public LogPlayViewModel(int gameId)
        {
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                CurrentPlayItem.BoardGameId = gameId;
                LoadData();
            }
        }

        private void LoadData()
        {
            PlayersList = DataService.Instance.LoadPlayers();
        }

        private PlayDataItem _CurrentPlayItem = new PlayDataItem() { PlayDate = DateTime.Now, Players = new ObservableCollection<PlayerStatsDataItem>() };
        public PlayDataItem CurrentPlayItem
        {
            get
            {
                if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                {
                    return DesignDataService.LoadPlay();
                }
                return _CurrentPlayItem;
            }
            set
            {
                Set(ref _CurrentPlayItem, value);
            }
        }

        public ObservableCollection<PlayerDataItem> _PlayersList = new ObservableCollection<PlayerDataItem>();
        public ObservableCollection<PlayerDataItem> PlayersList
        {
            get
            {
                if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                {
                    return DesignDataService.LoadPlayersList();
                }
                return _PlayersList;
            }
            set
            {
                Set(ref _PlayersList, value);
                FilteredPlayersList = new ObservableCollection<PlayerDataItem>(PlayersList);
            }
        }

        public ObservableCollection<PlayerDataItem> _FilteredPlayersList = new ObservableCollection<PlayerDataItem>();
        public ObservableCollection<PlayerDataItem> FilteredPlayersList
        {
            get
            {
                return _FilteredPlayersList;
            }
            set
            {
                Set(ref _FilteredPlayersList, value);
            }
        }

        public void FilterPlayers(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                FilteredPlayersList = new ObservableCollection<PlayerDataItem>(
                    PlayersList.Where(p => p.Name.IndexOf(sender.Text, 0, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList());
            }
        }

        public void PlayerChoosen(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                var selectedPlayer = new PlayerStatsDataItem(args.ChosenSuggestion as PlayerDataItem);
                if (CurrentPlayItem.Players.Where(p => p.Name == selectedPlayer.Name).ToList().Count == 0)
                {
                    CurrentPlayItem.Players.Add(selectedPlayer);
                }
                //sender.Text = String.Empty;
            }
        }

        public DelegateCommand LogPlayCommand => new DelegateCommand(async () =>
        {
            if (await DataService.Instance.LogPlay(CurrentPlayItem.BoardGameId, CurrentPlayItem.PlayDate, CurrentPlayItem.NumberOfPlays, CurrentPlayItem.UserComment, CurrentPlayItem.Length, CurrentPlayItem))
            {
                Messenger.Default.Send<RefreshDataMessage>(new RefreshDataMessage() { RequestedRefreshScope = RefreshDataMessage.RefreshScope.Plays });
            }
            else
            {
                Messenger.Default.Send(new StatusMessage()
                {
                    Status = StatusMessage.StatusType.Error,
                    Message = "There was an error while logging play"
                });
            }
        });

        public void SelectedDateChanged(CalendarDatePicker cal, CalendarDatePickerDateChangedEventArgs args)
        {
            CurrentPlayItem.PlayDate = args.NewDate.Value.DateTime;
        }
    }
}
