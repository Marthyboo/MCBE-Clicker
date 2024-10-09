using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography; // Import for RNGCryptoServiceProvider
using System.Threading;

class Program
{
    // DLL Imports
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    // Constants
    const int VK_C = 0x43; // 'C' key virtual key code
    const int VK_L = 0x4C; // 'L' key virtual key code
    const int VK_LBUTTON = 0x01; // Left mouse button virtual key code
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;

    static Random rand = new Random(); // Random generator for CPS fluctuation

    // Generate a random double between 0.0 and 1.0
    private static double GenerateRandomDouble()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            // Fill an array of bytes with 8 random bytes
            byte[] bytes = new byte[sizeof(double)];
            rng.GetBytes(bytes);
            // Bit-shift 11 and 53 based on double's mantissa bits
            var ul = BitConverter.ToUInt64(bytes, 0) / (1 << 11);
            double d = ul / (double)(1UL << 53);
            return d;
        }
    }

    // Generate a random integer within a range using linear interpolation
    public static int NextIntLinear(int minValue, int maxValue)
    {
        double sample = GenerateRandomDouble();
        return (int)(maxValue * sample + minValue * (1d - sample));
    }

    static void Main()
    {
        bool isClickerActive = false; // Track whether the auto-clicker is active
        bool randomizerActive = false; // Track whether the random CPS fluctuation is active
        int baseCps = 16; // Set initial CPS (clicks per second)
        int cps = baseCps; // Current CPS
        int delay = 1000 / cps; // Delay between clicks (milliseconds)

        IntPtr minecraftWindowHandle = FindWindow(null, "Minecraft");

        Console.WriteLine($"AutoClicker started. Press 'C' to toggle on/off. Press 'L' to toggle random CPS fluctuation. Initial CPS: {baseCps}");

        while (true)
        {
            // Check if the 'C' key is pressed to toggle the auto-clicker
            if (GetAsyncKeyState(VK_C) < 0)
            {
                isClickerActive = !isClickerActive; // Toggle the clicker state
                Thread.Sleep(200); // Prevent rapid toggling from key press
            }

            // Check if the 'L' key is pressed to toggle the randomizer
            if (GetAsyncKeyState(VK_L) < 0)
            {
                randomizerActive = !randomizerActive; // Toggle the randomizer state
                Console.WriteLine(randomizerActive ? "Random CPS fluctuation activated." : "Random CPS fluctuation deactivated.");
                Thread.Sleep(200); // Prevent rapid toggling from key press
            }

            // Get the current foreground window handle
            IntPtr activeWindow = GetForegroundWindow();

            // Only click if Minecraft is the active window and the clicker is active
            if (isClickerActive && activeWindow == minecraftWindowHandle && GetAsyncKeyState(VK_LBUTTON) < 0)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);

                // Click repeatedly while the left mouse button is held down
                while (GetAsyncKeyState(VK_LBUTTON) < 0 && isClickerActive && activeWindow == minecraftWindowHandle)
                {
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);

                    // Check if randomizer is active for CPS fluctuation
                    if (randomizerActive && GenerateRandomDouble() < 0.22) // 22% chance to fluctuate CPS
                    {
                        int fluctuation = NextIntLinear(-1, 2); // Randomly choose -1, 0, or 1
                        cps += fluctuation; // Update CPS

                        // Ensure CPS stays within reasonable limits (e.g., 1 to 50)
                        if (cps < 1) cps = 1;
                        if (cps > 50) cps = 50;

                        Console.WriteLine($"CPS adjusted to {cps}");
                    }

                    // Recalculate delay based on the new CPS
                    delay = 1000 / cps;
                    Thread.Sleep(delay); // Adjustable delay based on CPS

                    // Update the active window in the loop to prevent unintended clicks if the user switches windows
                    activeWindow = GetForegroundWindow();
                }

                // Release the button when no longer held
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                // Reset CPS to the base value after the click session ends
                cps = baseCps;
            }

            // Add a short delay to prevent high CPU usage
            Thread.Sleep(10);
        }
    }
}
