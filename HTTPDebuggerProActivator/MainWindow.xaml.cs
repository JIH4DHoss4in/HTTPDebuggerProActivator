using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace HTTPDebuggerProActivator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private string? _parsedVersion; // Stores the parsed application version.
    private string? _serialNumber; // Stores the generated serial number.
    private string? _generatedKey; // Stores the generated license key.
    
    public MainWindow()
    {
        InitializeComponent();
        ActivatorButton.IsEnabled = false; // Disable activator button initially.

        try
        {
            InitializeAppData(); // Initialize application data on startup.
        }
        catch (Exception ex)
        {
            KeyGenButton.IsEnabled = false; // Disable key generation button in case of failure.
            ShowMessage(ex.Message, Brushes.Red); // Display error message in UI.
        }
    }
    
    // Initializes application data by retrieving version and generating serial number.
    private void InitializeAppData()
    {
        _parsedVersion = GetAppVersion();
        _serialNumber = GenerateSerialNumber();
        SerialNumberTextBlock.Text = "SN" + _serialNumber; // Display serial number.
    }
    
    // Retrieves the installed version of HTTP Debugger Pro from the Windows registry.
    private string GetAppVersion()
    {
        const string registryPath = @"SOFTWARE\MadeForNet\HTTPDebuggerPro";
        using var key = Registry.CurrentUser.OpenSubKey(registryPath);

        if (key == null)
            throw new Exception("HTTPDebuggerPro not found in registry!");

        var version = key.GetValue("AppVer") as string;
        if (string.IsNullOrEmpty(version))
            throw new Exception("App version not found!");

        // Display formatted version in UI
        AppVersion.Text = new string(version.Where(c => char.IsDigit(c) || c == '.').ToArray());
        
        // Return only the numeric version
        return new string(version.Where(char.IsDigit).ToArray());
    }
    
    // Generates a serial number based on the volume serial number and application version.
    private string GenerateSerialNumber()
    {
        var volumeInfo = GetVolumeSerialNumber("C:\\");
        if (_parsedVersion == null)
            throw new Exception("Failed to get app version!");

        var version = int.Parse(_parsedVersion);
        
        // XOR-based algorithm for serial number generation
        var serialNumber = (uint)(version ^ ((~volumeInfo >> 1) + 0x2E0) ^ 0x590D4);
        return serialNumber.ToString();
    }
    
    // Generates a 16-character license key using a random byte-based approach.
    private void CreateKey()
    {
        var random = new Random();
        var keyBuilder = new StringBuilder();

        while (keyBuilder.Length < 16)
        {
            var v1 = (byte)random.Next(0, 256);
            var v2 = (byte)random.Next(0, 256);
            var v3 = (byte)random.Next(0, 256);
            
            keyBuilder.Append(
                $"{v1:X2}{v2 ^ 0x7C:X2}{(byte)~v1:X2}7C{v2:X2}{v3 % 255:X2}{(byte)(v3 % 255) ^ 7:X2}{v1 ^ (byte)~(v3 % 255):X2}");
        }

        _generatedKey = keyBuilder.ToString();
    }
    
    // Handles key generation button click.
    private void KeyGenButton_Click(object sender, RoutedEventArgs e)
    {
        SuccessErrorMessage.Visibility = Visibility.Collapsed;
        CreateKey();
        LicenseKey.Text = _generatedKey;
        LicenseKey.Visibility = Visibility.Visible;
        ActivatorButton.IsEnabled = true;
    }
    
    // Handles activation button click by writing generated key to the registry.
    private void ActivatorButton_Click(object sender, RoutedEventArgs e)
    {
        const string registryPath = @"SOFTWARE\MadeForNet\HTTPDebuggerPro";

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(registryPath);
            if (_generatedKey != null)
                key.SetValue("SN" + _serialNumber, _generatedKey);
            ShowMessage("Key successfully activated!", Brushes.LimeGreen);
        }
        catch
        {
            ShowMessage("Failed to create or open the registry key!", Brushes.Red);
        }
    }
    
    // Retrieves the volume serial number of a given drive.
    private static uint GetVolumeSerialNumber(string driveLetter)
    {
        if (!driveLetter.EndsWith('\\'))
            driveLetter += "\\";

        uint volumeSerial = 0;
        if (!NativeMethods.GetVolumeInformation(driveLetter, null, 0, ref volumeSerial, out _, out _, null, 0))
            throw new Exception("Failed to get volume serial number!");

        return volumeSerial;
    }
    
    // Displays a message in the UI.
    private void ShowMessage(string message, Brush color)
    {
        SuccessErrorMessage.Text = message;
        SuccessErrorMessage.Foreground = color;
        SuccessErrorMessage.Visibility = Visibility.Visible;
    }
    
    // Opens the Telegram channel link.
    private void TelegramButton_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://t.me/BDEvilZone");
    }
    
    // Opens the GitHub repository link.
    private void GitHubButton_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/JIH4DHoss4in/HTTPDebuggerProActivator");
    }
    
    // Opens a specified URL in the default web browser.
    private static void OpenUrl(string url)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    
    // Enables window dragging by clicking anywhere.
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        DragMove();
    }
    
    // Closes the application when the exit button is clicked.
    private void ExitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    
    // Native methods for accessing Windows API functions.
    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern bool GetVolumeInformation(string lpRootPathName, StringBuilder? lpVolumeNameBuffer, int nVolumeNameSize,
            ref uint lpVolumeSerialNumber, out uint lpMaximumComponentLength, out uint lpFileSystemFlags, StringBuilder? lpFileSystemNameBuffer, int nFileSystemNameSize);
    }
}