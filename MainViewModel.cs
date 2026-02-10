using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace BeatCfgMaker
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> _selectedFiles;
        private bool _isPlaying;
        private double _playbackSpeed;
        private string _currentAudioFile;
        private int _cycleCount;
        private string _recordInfo;
        private ObservableCollection<BeatRecord> _beatRecords;
        private BeatRecord _currentRecord;
        private bool _isRecording;
        private DateTime _startTime;
        private bool _canInsertNewBeat;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler PlaybackSpeedChanged;
        public event EventHandler<string> PlayRequested;
        public event EventHandler PauseRequested;
        public event EventHandler StartRecording;
        public event EventHandler StopRecording;
        public event EventHandler ScrollToBottomRequested;

        public RelayCommand SelectFileCommand { get; private set; }
        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }
        public RelayCommand StartConfigCommand { get; private set; }
        public RelayCommand InsertNewBeatCommand { get; private set; }
        public RelayCommand SaveFileCommand { get; private set; }
        
        public ObservableCollection<string> SelectedFiles
        {
            get => _selectedFiles;
            set
            {
                _selectedFiles = value;
                OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                _playbackSpeed = value;
                OnPropertyChanged();
                PlaybackSpeedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string CurrentAudioFile
        {
            get => _currentAudioFile;
            set
            {
                _currentAudioFile = value;
                OnPropertyChanged();
            }
        }

        public int CycleCount
        {
            get => _cycleCount;
            set
            {
                _cycleCount = value;
                OnPropertyChanged();
            }
        }

        public string RecordInfo
        {
            get => _recordInfo;
            set
            {
                _recordInfo = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<BeatRecord> BeatRecords
        {
            get => _beatRecords;
            set
            {
                _beatRecords = value;
                OnPropertyChanged();
            }
        }

        public bool CanInsertNewBeat
        {
            get => _canInsertNewBeat;
            set
            {
                _canInsertNewBeat = value;
                OnPropertyChanged();
                // 通过属性变更通知来更新命令状态
                OnPropertyChanged(nameof(CanInsertNewBeat));
            }
        }

        public MainViewModel()
        {
            SelectedFiles = new ObservableCollection<string>();
            BeatRecords = new ObservableCollection<BeatRecord>();
            BeatRecords.CollectionChanged += BeatRecords_CollectionChanged;
            SelectFileCommand = new RelayCommand(SelectFile);
            PlayCommand = new RelayCommand(PlayAudio, CanPlayAudio);
            PauseCommand = new RelayCommand(PauseAudio, CanPauseAudio);
            StartConfigCommand = new RelayCommand(StartBeatConfig, CanStartBeatConfig);
            InsertNewBeatCommand = new RelayCommand(InsertNewBeat, CanInsertNewBeatFunc);
            SaveFileCommand = new RelayCommand(SaveBeatRecords, CanSaveBeatRecords);
            PlaybackSpeed = 1.0; // 默认正常速度
            CycleCount = 4; // 默认监听次数
            RecordInfo = "节奏记录"; // 默认记录信息
            CanInsertNewBeat = true; // 默认可以插入新节奏
        }

        private void BeatRecords_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 当BeatRecords集合发生变化时，请求滚动到底部
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SelectFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "音频文件 (*.mp3;*.wav;*.flac;*.aac;*.ogg)|*.mp3;*.wav;*.flac;*.aac;*.ogg|所有文件 (*.*)|*.*",
                Title = "选择音乐文件",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFiles.Clear();
                SelectedFiles.Add(openFileDialog.FileName);
                CurrentAudioFile = openFileDialog.FileName;
                IsPlaying = false;
                
                MessageBox.Show($"已选择文件：\\n{openFileDialog.FileName}", "文件选择成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PlayAudio()
        {
            if (!string.IsNullOrEmpty(CurrentAudioFile))
            {
                PlayRequested?.Invoke(this, CurrentAudioFile);
                IsPlaying = true;
                
                // 如果正在录制，设置开始时间
                if (_isRecording)
                {
                    _startTime = DateTime.Now;
                }
            }
        }

        private bool CanPlayAudio()
        {
            return !string.IsNullOrEmpty(CurrentAudioFile) && !IsPlaying;
        }

        private void PauseAudio()
        {
            if (!string.IsNullOrEmpty(CurrentAudioFile) && IsPlaying)
            {
                PauseRequested?.Invoke(this, EventArgs.Empty);
                IsPlaying = false;
            }
        }

        private bool CanPauseAudio()
        {
            return !string.IsNullOrEmpty(CurrentAudioFile) && IsPlaying;
        }

        private void StartBeatConfig()
        {
            // 如果没有当前记录，创建新的节奏记录
            if (_currentRecord == null)
            {
                _currentRecord = new BeatRecord
                {
                    CycleCount = CycleCount,
                    RecordInfo = RecordInfo,
                    Timestamps = new ObservableCollection<TimeSpan>()
                };
                BeatRecords.Add(_currentRecord);
            }
            
            _isRecording = true;
            CanInsertNewBeat = false; // 配置时无法插入新节奏
            StartRecording?.Invoke(this, EventArgs.Empty);
            
            MessageBox.Show("开始节奏配置，播放音乐后按空格键记录时间点", "开始配置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanStartBeatConfig()
        {
            return !string.IsNullOrEmpty(CurrentAudioFile) && !_isRecording;
        }

        private void InsertNewBeat()
        {
            // 创建新的节奏记录
            var newRecord = new BeatRecord
            {
                CycleCount = CycleCount,
                RecordInfo = RecordInfo,
                Timestamps = new ObservableCollection<TimeSpan>()
            };
            
            BeatRecords.Add(newRecord);
            
            // 如果当前没有在配置，则设置新插入的节奏为当前配置节奏
            if (!_isRecording)
            {
                _currentRecord = newRecord;
            }
            
            MessageBox.Show($"已插入新节奏：{RecordInfo}，循环次数：{CycleCount}", "插入成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanInsertNewBeatFunc()
        {
            return CanInsertNewBeat && !string.IsNullOrEmpty(RecordInfo) && CycleCount > 0;
        }

        private void SaveBeatRecords()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "节奏配置文件 (*.beatcfg)|*.beatcfg|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                Title = "保存节奏配置",
                DefaultExt = ".beatcfg"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        // 开始大数组格式
                        writer.WriteLine("beatRecords = {");
                        
                        foreach (var record in BeatRecords)
                        {
                            // 格式：beat{key = 循环次数,value = {时间点1}{时间点2}{...}}
                            writer.Write($"    beat{{key = {record.CycleCount}, value = {{");
                            
                            if (record.Timestamps != null && record.Timestamps.Count > 0)
                            {
                                foreach (var timestamp in record.Timestamps)
                                {
                                    writer.Write($"{{{timestamp.TotalMilliseconds}}}");
                                }
                            }
                            
                            writer.Write("}}");
                            
                            // 如果不是最后一个记录，添加逗号分隔
                            if (record != BeatRecords[BeatRecords.Count - 1])
                            {
                                writer.Write(",");
                            }
                            writer.WriteLine();
                        }
                        
                        writer.WriteLine("}");
                    }
                    
                    MessageBox.Show($"节奏配置已保存到：{saveFileDialog.FileName}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件时出错：{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanSaveBeatRecords()
        {
            return BeatRecords != null && BeatRecords.Count > 0;
        }

        // 空格键按下事件处理
        public void HandleSpaceKeyPress()
        {
            if (_isRecording && IsPlaying)
            {
                // 如果当前记录为空，创建新的节奏记录
                if (_currentRecord == null)
                {
                    _currentRecord = new BeatRecord
                    {
                        CycleCount = CycleCount,
                        RecordInfo = RecordInfo,
                        Timestamps = new ObservableCollection<TimeSpan>()
                    };
                    BeatRecords.Add(_currentRecord);
                }
                
                var currentTime = DateTime.Now - _startTime;
                _currentRecord.Timestamps.Add(currentTime);
                
                // 如果达到循环次数，标记当前记录完成，下次按键时创建新记录
                if (_currentRecord.Timestamps.Count >= _currentRecord.CycleCount)
                {
                    // 在当前记录信息中添加完成标记
                    if (!_currentRecord.RecordInfo.Contains("（循环完成）"))
                    {
                        _currentRecord.RecordInfo += "（循环完成）\\n";
                    }
                    
                    // 将当前记录设为null，下次按键时创建新记录
                    _currentRecord = null;
                }
            }
        }

        // 停止录制
        public void StopBeatRecording()
        {
            _isRecording = false;
            _currentRecord = null;
            CanInsertNewBeat = true; // 停止录制后可以插入新节奏
            StopRecording?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 节奏记录类
    public class BeatRecord
    {
        public int CycleCount { get; set; }
        public string RecordInfo { get; set; }
        public ObservableCollection<TimeSpan> Timestamps { get; set; }
    }
}