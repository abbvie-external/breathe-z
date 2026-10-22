using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using AutomationEngineering.CommonTools.ExtensionMethods;
using AutomationEngineering.LoggingHelper;
using BreatheZ.Ui.Annotations;
using BreatheZ.Ui.MotionControl;
using BreatheZ.Ui.Properties;
using BreatheZ.Ui.Util;
using log4net;

namespace BreatheZ.Ui.Engine
{
    public class Runner : INotifyPropertyChanged
    {
        private static readonly ILog log = LogCreator.ClassLogger();
        private string currentStatus;
        private UnitOfWork currentUnit;
        private bool isRunning;

        public Action PauseAction;

        public string CurrentStatus
        {
            get { return currentStatus; }
            set
            {
                if (value == currentStatus) return;
                currentStatus = value;
                OnPropertyChanged();
                log.Info("Current Status:" + IsRunning);
            }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                Warning.WarningOn = (IsRunning);

                if (value.Equals(isRunning)) return;

                isRunning = value;
                OnPropertyChanged();
                log.Info("IsRunning Toggle:" + IsRunning);
            }
        }

        public MotionWarning Warning { get; private set; }

        public ObservableCollection<UnitOfWork> WorkList { get; private set; }

        public IEnumerable<UnitOfWork> PendingWorkList
        {
            get { return WorkList.Where(wl => wl.Status != WorkStatus.Completed && wl.Display).ToList(); }
        }
        
        public UnitOfWork CurrentUnit
        {
            get { return currentUnit; }
            set
            {
                if (Equals(value, currentUnit)) return;
                currentUnit = value;
                OnPropertyChanged();

                if (CurrentUnit != null)
                {
                    log.Info("Current Unit Of Work:" + CurrentUnit?.Display);
                }
            }
        }
        public DateTime? LastTimeCheck { get; set; }

        public void PopNextUnit()
        {
            if (WorkList.Any())
            {
                CurrentUnit = WorkList[0];
                WorkList.RemoveAt(0);

                Task.Factory.StartNew(CurrentUnit.Run);
            }
        }

        public void Clear()
        {
            WorkList.Clear();
            CurrentStatus = string.Empty;
        }

        public Runner(MotionWarning warning, Action pauseAction)
        {
            PauseAction = pauseAction;
            Warning = warning;
            WorkList = new ObservableCollection<UnitOfWork>();
            WorkList.CollectionChanged += WorkList_CollectionChanged;

            Task.Run(()=> RunLoop());
        }

        void WorkList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("WorkList");
            OnPropertyChanged("PendingWorkList");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RunLoop()
        {
            while (true)
            {
                if (!IsRunning) continue;

                if (WorkList.All(wl => wl.Status != WorkStatus.Queued))
                {
                    IsRunning = false;
                    continue;
                }

                if (CurrentUnit == null)
                {
                    log.Info("Pulling next unit of work");

                    CurrentUnit = WorkList.First();

                    if (CurrentUnit.PauseOnStart)
                    {
                        IsRunning = false;
                        CurrentStatus = CurrentUnit.StatusMessage;
                        PauseAction();
                        continue;
                    }
                }

                if (CurrentUnit.Status == WorkStatus.Running)
                {
                    continue;
                }

                if (CurrentUnit.Status == WorkStatus.Error)
                {
                    CurrentUnit.Status = WorkStatus.Queued;
                }

                if (CurrentUnit.Status == WorkStatus.Completed)
                {
                    var first = WorkList.FirstOrDefault(uow => uow.Status == WorkStatus.Queued);

                    if (first == null)
                    {
                        Task.Delay(500).Wait();
                        IsRunning = false;

                        OnPropertyChanged("PendingWorkList");

                        CurrentUnit = null;
                    }
                    else
                    {
                        OnPropertyChanged("PendingWorkList");

                        CurrentUnit = first;
                        
                        if (CurrentUnit.PauseOnStart)
                        {
                            IsRunning = false;
                            CurrentStatus = CurrentUnit.StatusMessage;
                            PauseAction();
                            continue;
                        }
                    }
                }

                if (CurrentUnit!= null && CurrentUnit.Status == WorkStatus.Queued)
                {
                    CurrentStatus = string.Empty;


                    if (CurrentUnit.TimeCheckPoint && LastTimeCheck != null)
                    {
                        CurrentStatus = "Waiting for Time Check Point";

                        while (DateTime.Now.Subtract((DateTime) LastTimeCheck).TotalSeconds < Settings.Default.TimeCheckPointInSeconds)
                        {
                            Task.Delay(100).Wait();
                        }

                        CurrentStatus = string.Empty;
                    }

                    if (!IsRunning)
                    {
                        CurrentStatus = string.Empty;
                        continue;
                    }

                    if (CurrentUnit.TimeCheckPoint)
                    {
                        LastTimeCheck = DateTime.Now;
                    }

                    try
                    {
                        CurrentUnit.Run();
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error when processing " + CurrentUnit.WorkDescription,ex);
                        CurrentUnit.Status = WorkStatus.Error;
                        CurrentStatus = "Error:" + ex.Message;

                        Speak(CurrentStatus);

                        IsRunning = false;
                    }
                    
                    OnPropertyChanged("PendingWorkList");
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void Speak(string text)
        {
            Task.Run(() =>
                     {
                         using (var reader = Talker.GetSpeechSynthesizer())
                         {
                             reader.Rate = -3;
                             reader.Speak(text.Replace('_', ' '));
                         }
                     });
        }
    }
}
