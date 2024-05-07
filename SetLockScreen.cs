using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime; // 对IAsyncOperation的支持



namespace _92CloudWallpaper
{
    public static class SetLockScreen
    {
        public static async Task SetImageAsync(string imagePath)
        {
            Version osVersion = Environment.OSVersion.Version;
            Console.WriteLine(osVersion.Major);
            if (osVersion.Major >= 10)
            {
                // Windows 10及以上
                await SetLockScreenImageWin10(imagePath);
            }
            else if (osVersion.Major == 6 && osVersion.Minor >= 1)
            {
                // Windows 7/8
                SetLockScreenImageWin10(imagePath);
            }
            else
            {
                Console.WriteLine("Unsupported Windows version.");
            }
        }

        public static async Task SetLockScreenImageWin10(string imagePath)
        {
            try
            {
                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(imagePath);
                var result = await Windows.System.UserProfile.UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(storageFile);
                Console.WriteLine(result ? "Lock screen image updated successfully." : "Failed to update lock screen image.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }



        private static void SetLockScreenImageWin7(string imagePath)
        {
            try
            {
                string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Background";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKeyPath, writable: true) ?? Registry.LocalMachine.CreateSubKey(registryKeyPath))
                {
                    key.SetValue("OEMBackground", 1, RegistryValueKind.DWord);
                }

                string oobePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"oobe\info\backgrounds");
                if (!Directory.Exists(oobePath))
                {
                    Directory.CreateDirectory(oobePath);
                }

                string destPath = Path.Combine(oobePath, "backgroundDefault.jpg");
                File.Copy(imagePath, destPath, true);

                Console.WriteLine("Lock screen image updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
