using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.Windows.Forms.NotifyIcon

namespace FileCopyApp
{
    public partial class MainWindow : Window
    {
        private string inputPath;
        private string outputPath;
        private FileSystemWatcher watcher;
        private Timer timer;
        private Dictionary<string, DateTime> changedFiles;
        private Popup notificationPopup;

        public MainWindow()
        {
            InitializeComponent();
            inputTextBox.Text = "D:\\excel";
            outputTextBox.Text = "D:\\Users";
            timer = new Timer(1000); // Timer kiểm tra mỗi giây
            timer.Elapsed += TimerElapsed;
            changedFiles = new Dictionary<string, DateTime>();
            // Khởi tạo Popup thông báo
            notificationPopup = new Popup();
            notificationPopup.Placement = PlacementMode.Absolute;
            notificationPopup.PlacementTarget = this;
            notificationPopup.AllowsTransparency = true;
            notificationPopup.PopupAnimation = PopupAnimation.Fade;
            notificationPopup.StaysOpen = false;
            notificationPopup.Child = CreateNotificationContent();

            // Đặt vị trí Popup ở góc phải của màn hình
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            notificationPopup.HorizontalOffset = screenWidth - notificationPopup.Width;
            notificationPopup.VerticalOffset = screenHeight - notificationPopup.Height;

            // Ẩn ứng dụng khỏi thanh taskbar
            ShowInTaskbar = false;

        }
        private UIElement CreateNotificationContent()
        {
            // Tạo nội dung thông báo (ví dụ: hình vuông đỏ)
            var rectangle = new Rectangle();
            rectangle.Fill = Brushes.Red;
            rectangle.Width = 50;
            rectangle.Height = 50;

            return rectangle;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Xử lý các tệp đã thay đổi trong thư mục
            ProcessChangedFiles();
        }

        private void StartWatching_Click(object sender, RoutedEventArgs e)
        {
            inputPath = inputTextBox.Text;
            outputPath = outputTextBox.Text;

            if (!Directory.Exists(inputPath))
            {
                MessageBox.Show("Thư mục không tồn tại.");
                return;
            }

            // Khởi tạo FileSystemWatcher
            watcher = new FileSystemWatcher(inputPath);
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            // Đăng ký các sự kiện
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Deleted += Watcher_Changed;
            watcher.Renamed += Watcher_Renamed;

            // Khởi động Timer
            timer.Start();

            startWatchingButton.IsEnabled = false;
            stopWatchingButton.IsEnabled = true;
        }

        private void StopWatching_Click(object sender, RoutedEventArgs e)
        {
            // Dừng FileSystemWatcher và Timer
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            timer.Stop();

            startWatchingButton.IsEnabled = true;
            stopWatchingButton.IsEnabled = false;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                // Nếu đối tượng mới là thư mục, tạo thư mục đích
                string relativePath = Path.GetRelativePath(inputPath, e.FullPath);
                string destinationPath = Path.Combine(outputPath, relativePath);
                Directory.CreateDirectory(destinationPath);
            }
            else if (File.Exists(e.FullPath))
            {
                // Nếu đối tượng mới là tệp tin, lưu thời gian thay đổi
                changedFiles[e.FullPath] = DateTime.Now;
            }
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            // Lưu thời gian thay đổi của tệp đã đổi tên và tệp mới
            changedFiles[e.OldFullPath] = DateTime.Now;
            // Xóa thông tin về tệp cũ khỏi changedFiles
            changedFiles.Remove(e.OldFullPath);
            changedFiles[e.FullPath] = DateTime.Now;

        }

        private void ProcessChangedFiles()
        {
            List<string> filesToCopy = new List<string>();

            // Kiểm tra và lọc các tệp đã thay đổi trong khoảng thời gian gần đây
            DateTime threshold = DateTime.Now.AddSeconds(-1);
            foreach (var entry in changedFiles)
            {
                if (entry.Value > threshold)
                {
                    filesToCopy.Add(entry.Key);
                }
            }

            // Sao chép các tệp đã thay đổi
            if (filesToCopy.Count > 0)
            {
                try
                {
                    foreach (string file in filesToCopy)
                    {
                        string relativePath = Path.GetRelativePath(inputPath, file);
                        string destinationPath = Path.Combine(outputPath, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        File.Copy(file, destinationPath, true);
                    }

                    Dispatcher.Invoke(() => MessageBox.Show("Sao chép tệp thành công!"));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Lỗi sao chép tệp: {ex.Message}"));
                }
            }
        }
    }

}