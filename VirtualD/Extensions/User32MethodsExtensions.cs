using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace VirtualD.Extensions
{
    public static class User32Extensions
    {
        public static IEnumerable<HMONITOR> GetMonitors()
        {
            var monitors = new List<HMONITOR>();

            unsafe
            {
                EnumDisplayMonitors(
                    null,
                    null,
                    (hMonitor, _, _, _) =>
                    {
                        monitors.Add(hMonitor);
                        return true;
                    }, 
                    0);
            }

            return monitors;
        }
        
        public static List<DISPLAY_DEVICEW> GetDisplayDevices()
        {
            var devices = new List<DISPLAY_DEVICEW>();

            Console.WriteLine("\nDISPLAYS");

            var i = 0u;
            var device = new DISPLAY_DEVICEW();
            unsafe
            {
                device.cb = (uint) sizeof(DISPLAY_DEVICEW);
            }

            while (EnumDisplayDevices(null, i, ref device, 0))
            {
                if (((DisplayDeviceStateFlags) device.StateFlags).HasFlag(DisplayDeviceStateFlags.AttachedToDesktop))
                {
                    devices.Add(device);
                }

                i++;
            }

            return devices;
        }
        
        public static HMONITOR MonitorFromCursor(MONITOR_FROM_FLAGS dwFlags)
        {
            GetCursorPos(out var point);
            return MonitorFromPoint(point, dwFlags);
        }

        public static IEnumerable<HWND> FindWindows(WNDENUMPROC filter)
        {
            var windows = new List<HWND>();

            EnumWindows((wnd, param) =>
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }

                // but return true here so that we iterate all windows
                return true;
            },
            IntPtr.Zero);

            return windows;
        }

        public static string GetWindowText(HWND handle)
        {
            var size = GetWindowTextLength(handle);

            if (size <= 0)
            {
                return string.Empty;
            }
            
            unsafe
            {
                fixed (char* text = new char[size])
                {
                    PInvoke.GetWindowText(handle, text, size + 1);

                    return new string(text);
                }
            }
        }

        public static string GetClassName(HWND hWnd)
        {
            var size = 16;

            var name = new PWSTR();
            PInvoke.GetClassName(hWnd, name, size);
            return name.ToString();
        }
    }
}
