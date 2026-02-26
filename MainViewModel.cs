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
                
                // 点击播放时设置开始时间，确保节拍时间戳与音乐同步
                if (_isRecording && _startTime == DateTime.MinValue)
                {
                    _startTime = DateTime.Now;
                }
            }
        }
        
        // 媒体真正开始播放时调用
        public void OnMediaOpened()
        {
            // 如果正在录制且开始时间未设置，设置开始时间
            if (_isRecording && _startTime == DateTime.MinValue)
            {
                _startTime = DateTime.Now;
            }
        }

        private bool CanPlayAudio()
        {
            return !string.IsNullOrEmpty(CurrentAudioFile) && !IsPlaying && _isRecording;
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
            return !string.IsNullOrEmpty(CurrentAudioFile) && IsPlaying && _isRecording;
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
            _startTime = DateTime.MinValue; // 开始时间将在播放时设置，确保与音乐同步
            CanInsertNewBeat = false; // 配置时无法插入新节奏
            StartRecording?.Invoke(this, EventArgs.Empty);
            
            MessageBox.Show("开始节奏配置，现在可以播放音乐并按空格键记录时间点", "开始配置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanStartBeatConfig()
        {
            return !string.IsNullOrEmpty(CurrentAudioFile) && !_isRecording;
        }

        private void InsertNewBeat()
        {
            // 如果当前正在录制，先停止录制
            if (_isRecording)
            {
                StopBeatRecording();
            }
            
            // 创建新的节奏记录
            var newRecord = new BeatRecord
            {
                CycleCount = CycleCount,
                RecordInfo = RecordInfo,
                Timestamps = new ObservableCollection<TimeSpan>()
            };
            
            BeatRecords.Add(newRecord);
            
            // 设置新插入的节奏为当前配置节奏
            _currentRecord = newRecord;
            
            MessageBox.Show($"已插入新节奏：{RecordInfo}，循环次数：{CycleCount}", "插入成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanInsertNewBeatFunc()
        {
            return CanInsertNewBeat && !string.IsNullOrEmpty(RecordInfo) && CycleCount > 0;
        }

        /// <summary>
        /// 计算单个节奏型内部相邻时间点的最短间隔（毫秒）。
        /// 若时间点不足2个则返回 double.MaxValue。
        /// </summary>
        private double GetMinInterval(BeatRecord record)
        {
            if (record.Timestamps == null || record.Timestamps.Count < 2)
                return double.MaxValue;

            // 将时间点排序后计算相邻差值
            var sorted = new System.Collections.Generic.List<double>();
            foreach (var ts in record.Timestamps)
                sorted.Add(ts.TotalMilliseconds);
            sorted.Sort();

            double minInterval = double.MaxValue;
            for (int i = 1; i < sorted.Count; i++)
            {
                double diff = sorted[i] - sorted[i - 1];
                if (diff < minInterval)
                    minInterval = diff;
            }
            return minInterval;
        }

        /// <summary>
        /// 对所有节奏记录的时间戳进行跨节奏型对齐格式化。
        /// 容差 = 所有节奏型中最短输入时间间隔的最小值；
        /// 两个跨节奏型时间点的差值 &lt; 容差时，统一替换为它们的平均值。
        /// </summary>
        private void AlignTimestamps()
        {
            if (BeatRecords == null || BeatRecords.Count < 2) return;

            // 1. 计算每个节奏型的最短间隔，取全局最小值作为容差
            double globalMinInterval = double.MaxValue;
            foreach (var record in BeatRecords)
            {
                double minInterval = GetMinInterval(record);
                if (minInterval < globalMinInterval)
                    globalMinInterval = minInterval;
            }

            // 所有节奏型都只有1个时间点，无法计算间隔，跳过对齐
            if (globalMinInterval == double.MaxValue) return;

            double toleranceMs = globalMinInterval;

            // 2. 收集所有时间戳（毫秒），并记录来源
            var allTimestamps = new System.Collections.Generic.List<(int RecordIndex, int TimestampIndex, double Ms)>();
            for (int i = 0; i < BeatRecords.Count; i++)
            {
                var record = BeatRecords[i];
                if (record.Timestamps == null) continue;
                for (int j = 0; j < record.Timestamps.Count; j++)
                {
                    allTimestamps.Add((i, j, record.Timestamps[j].TotalMilliseconds));
                }
            }

            // 3. 按时间排序，对相近的时间节点进行分组
            allTimestamps.Sort((a, b) => a.Ms.CompareTo(b.Ms));

            var groups = new System.Collections.Generic.List<System.Collections.Generic.List<(int RecordIndex, int TimestampIndex, double Ms)>>();
            foreach (var ts in allTimestamps)
            {
                bool added = false;
                foreach (var group in groups)
                {
                    // 与组内最大值比较（已排序，最后一个最大），差值 < 容差才考虑归入同组
                    if (ts.Ms - group[group.Count - 1].Ms < toleranceMs)
                    {
                        // 关键：同一节奏型的时间点不能归入同一组，防止同节奏内部被错误对齐
                        bool alreadyHasSameRecord = false;
                        foreach (var item in group)
                        {
                            if (item.RecordIndex == ts.RecordIndex)
                            {
                                alreadyHasSameRecord = true;
                                break;
                            }
                        }
                        if (!alreadyHasSameRecord)
                        {
                            group.Add(ts);
                            added = true;
                            break;
                        }
                    }
                }
                if (!added)
                {
                    groups.Add(new System.Collections.Generic.List<(int, int, double)> { ts });
                }
            }

            // 4. 对每个分组，若包含来自不同节奏型的时间点，则统一为平均值
            int alignedCount = 0;
            foreach (var group in groups)
            {
                if (group.Count <= 1) continue;

                // 检查是否来自不同节奏型
                bool hasMultipleSources = false;
                int firstRecordIndex = group[0].RecordIndex;
                foreach (var item in group)
                {
                    if (item.RecordIndex != firstRecordIndex)
                    {
                        hasMultipleSources = true;
                        break;
                    }
                }
                if (!hasMultipleSources) continue;

                // 计算平均值并更新
                double avgMs = 0;
                foreach (var item in group) avgMs += item.Ms;
                avgMs /= group.Count;
                double roundedMs = Math.Round(avgMs);

                foreach (var item in group)
                {
                    BeatRecords[item.RecordIndex].Timestamps[item.TimestampIndex] = TimeSpan.FromMilliseconds(roundedMs);
                }
                alignedCount++;
            }

            if (alignedCount > 0)
            {
                MessageBox.Show($"已对齐 {alignedCount} 个跨节奏型的时间节点（容差：< {Math.Round(toleranceMs)}ms，即最短输入间隔）", "时间对齐", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
                    // 保存前先对跨节奏型的时间节点进行对齐格式化
                    AlignTimestamps();

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
                                    writer.Write($"{{{Math.Round(timestamp.TotalMilliseconds)}}}");
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
                    
                    // 循环完成后重新启用插入新节奏功能
                    CanInsertNewBeat = true;
                }
            }
        }

        // 停止录制
        public void StopBeatRecording()
        {
            _isRecording = false;
            _currentRecord = null;
            _startTime = DateTime.MinValue; // 重置开始时间
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