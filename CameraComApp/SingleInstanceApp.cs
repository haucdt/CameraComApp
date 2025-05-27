using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;

namespace CameraComApp
{
    public static class SingleInstanceManager
    {
        // Windows API để quản lý cửa sổ
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private const int SW_RESTORE = 9;
        private static Mutex _mutex;
        private static string _mutexName;

        /// <summary>
        /// Khởi tạo và kiểm tra single instance với tên mutex.
        /// </summary>
        /// <param name="mutexName">Tên duy nhất cho mutex (khuyến nghị dùng GUID hoặc tên ứng dụng).</param>
        /// <param name="form">Form chính của ứng dụng (Windows Forms).</param>
        /// <returns>True nếu là instance đầu tiên, False nếu đã có instance khác.</returns>
        public static bool Initialize(string mutexName, Window window = null)
        {
            _mutexName = mutexName;
            bool createdNew;
            _mutex = new Mutex(true, _mutexName, out createdNew);

            if (!createdNew)
            {
                // Instance đã tồn tại, kích hoạt và căn giữa cửa sổ
                ActivateAndCenterWindow(window);
                return false;
            }

            // Lưu trữ form để sử dụng sau (nếu cần)
           AppDomain.CurrentDomain.ProcessExit += (s, e) => Release();
            return true;
        }

        /// <summary>
        /// Kích hoạt và đưa cửa sổ của instance hiện tại ra giữa màn hình.
        /// </summary>
        public static void ActivateAndCenterWindow(Window window )
        {
            IntPtr hWnd = FindWindowByProcess();
            if (hWnd != IntPtr.Zero)
            {
                
                ShowWindow(hWnd, SW_RESTORE); // Khôi phục nếu đang thu nhỏ
                SetForegroundWindow(hWnd); // Đưa cửa sổ ra trước
                CenterWindow(hWnd, window); // Căn giữa cửa sổ
              //  CenterWindow(window); // Căn giữa cửa sổ
            }
        }

        /// <summary>
        /// Tìm cửa sổ của instance đang chạy dựa trên tên process.
        /// </summary>
        private static IntPtr FindWindowByProcess()
        {
            var currentProcess = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                {
                    return process.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Đưa cửa sổ ra giữa màn hình.
        /// </summary>
        private static void CenterWindow(IntPtr hWnd, Window window)
        {
            // Lấy kích thước màn hình
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Giả sử kích thước cửa sổ (có thể tùy chỉnh)
            double windowWidth = window != null ? window.Width : 705.5; // Thay đổi tùy theo ứng dụng
            double windowHeight = window != null ? window.Height : 442.5 ;

            // Tính toán vị trí giữa màn hình
            int x = (int)(screenWidth - windowWidth) / 2;
            int y = (int)(screenHeight - windowHeight) / 2;

        //    int x = (int)((screenWidth / 2) - (windowWidth / 2));
         //   int y = (int)((screenHeight / 2) - (windowHeight / 2));

            // Di chuyển cửa sổ
            MoveWindow(hWnd, x, y, (int)windowWidth, (int)windowHeight, true);
        }

        private static void CenterWindow8(Window window)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = window.Width;
            double windowHeight = window.Height;

            // Tính toán vị trí giữa màn hình
            window.Left = (screenWidth / 2) - (windowWidth / 2);
            window.Top = (screenHeight / 2) - (windowHeight / 2);

        }
        /// <summary>
        /// Giải phóng Mutex khi ứng dụng đóng.
        /// </summary>
        public static void Release()
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }

    }
}