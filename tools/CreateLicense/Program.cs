using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static string GetHDDSerial()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia");
            foreach (ManagementObject mo in searcher.Get())
            {
                if (mo["SerialNumber"] != null)
                    return mo["SerialNumber"].ToString().Trim();
                break;
            }
        }
        catch { }
        return string.Empty;
    }

    static byte[] ProtectBytes(byte[] data)
    {
        try { return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser); }
        catch { return null; }
    }

    static int Main(string[] args)
    {
        var outPath = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "app.lic");
        int days = 365;
        if (args.Length > 1 && int.TryParse(args[1], out var d)) days = d;

        var hdd = GetHDDSerial();
        if (string.IsNullOrEmpty(hdd))
        {
            Console.WriteLine("Warning: HDD serial not detected. Using placeholder 'UNKNOWN'.");
            hdd = "UNKNOWN";
        }

        var expiry = DateTime.Now.AddDays(days).Ticks;
        var content = $"{hdd}|{expiry}";
        var bytes = Encoding.UTF8.GetBytes(content);
        var protectedBytes = ProtectBytes(bytes);
        if (protectedBytes == null)
        {
            Console.WriteLine("Failed to protect bytes.");
            return 2;
        }

        try
        {
            File.WriteAllBytes(outPath, protectedBytes);
            Console.WriteLine($"Wrote license to {outPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to write license: " + ex.Message);
            return 3;
        }
    }
}
