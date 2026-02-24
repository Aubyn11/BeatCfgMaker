using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BeatCfgMaker
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            var viewModel = new MainViewModel();
            DataContext = viewModel;
            
            // 订阅ViewModel的事件
            viewModel.PlayRequested += OnPlayRequested;
            viewModel.PauseRequested += OnPauseRequested;
            viewModel.PlaybackSpeedChanged += OnPlaybackSpeedChanged;
            viewModel.StartRecording += OnStartRecording;
            viewModel.StopRecording += OnStopRecording;
            viewModel.ScrollToBottomRequested += OnScrollToBottomRequested;
            
            // 初始化MediaElement
            AudioPlayer.MediaEnded += (s, e) => viewModel.IsPlaying = false;
            AudioPlayer.MediaFailed += (s, e) => 
            {
                viewModel.IsPlaying = false;
                MessageBox.Show($"播放失败：{e.ErrorException?.Message}", "播放错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            AudioPlayer.MediaOpened += (s, e) => 
            {
                // 媒体真正开始播放时通知ViewModel
                viewModel.OnMediaOpened();
            };
            
            // 添加键盘事件监听
            this.KeyDown += OnKeyDown;
            this.PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPlayRequested(object sender, string audioFile)
        {
            try
            {
                if (AudioPlayer.Source == null || AudioPlayer.Source.ToString() != audioFile)
                {
                    AudioPlayer.Source = new Uri(audioFile);
                }
                
                var viewModel = (MainViewModel)DataContext;
                AudioPlayer.SpeedRatio = viewModel.PlaybackSpeed;
                AudioPlayer.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"播放失败：{ex.Message}", "播放错误", MessageBoxButton.OK, MessageBoxImage.Error);
                ((MainViewModel)DataContext).IsPlaying = false;
            }
        }

        private void OnPauseRequested(object sender, EventArgs e)
        {
            if (AudioPlayer.Source != null && AudioPlayer.HasAudio)
            {
                AudioPlayer.Pause();
            }
        }

        private void OnPlaybackSpeedChanged(object sender, EventArgs e)
        {
            var viewModel = (MainViewModel)DataContext;
            AudioPlayer.SpeedRatio = viewModel.PlaybackSpeed;
        }
        
        private void OnStartRecording(object sender, EventArgs e)
        {
            // 开始录制时，确保窗口可以接收键盘事件
            this.Focusable = true;
            this.Focus();
        }
        
        private void OnStopRecording(object sender, EventArgs e)
        {
            // 停止录制
        }
        
        private void OnScrollToBottomRequested(object sender, EventArgs e)
        {
            // 延迟执行滚动操作，确保UI已经更新
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (BeatRecordsScrollViewer != null)
                {
                    BeatRecordsScrollViewer.ScrollToEnd();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // 处理空格键按下事件
            if (e.Key == Key.Space)
            {
                var viewModel = (MainViewModel)DataContext;
                viewModel.HandleSpaceKeyPress();
                e.Handled = true; // 阻止事件继续传播
            }
        }
        
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 在PreviewKeyDown中处理，确保在控件获得焦点前捕获事件
            if (e.Key == Key.Space)
            {
                var viewModel = (MainViewModel)DataContext;
                if (viewModel.IsPlaying)
                {
                    viewModel.HandleSpaceKeyPress();
                    e.Handled = true; // 阻止事件继续传播
                }
            }
        }
    }
}