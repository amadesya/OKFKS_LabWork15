using System.Management;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfActivationClient
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }

        private async void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            string licenseKey = LicenseKeyTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                ShowResult("Введите лицензионный ключ", Brushes.Red);
                return;
            }

            string hardwareId = GetHardwareId();

            ActivationRequest request = new(licenseKey, hardwareId);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("http://localhost:5183/activate", request);
                response.EnsureSuccessStatusCode();

                ActivationResponse result = await response.Content.ReadFromJsonAsync<ActivationResponse>() ?? new ActivationResponse(false, "Пустой ответ сервера");
                if (result.Success)
                {
                    ShowResult("Активация успешна", Brushes.Green);
                }
                else
                {
                    ShowResult($"Ошибка активации: {result.Message}", Brushes.Red);
                }
            }
            catch (HttpRequestException ex)
            {
                ShowResult($"Ошибка сети: {ex.Message}", Brushes.Red);
            }
        }

        private void ShowResult(string message, Brush color)
        {
            ResultTextBlock.Foreground = color;
            ResultTextBlock.Text = message;
        }

        private static string GetHardwareId()
        {
            string cpuId = string.Empty;
            using (ManagementObjectSearcher cpuSearcher = new("SELECT ProcessorId FROM Win32_Processor"))
            {
                ManagementObject? cpu = cpuSearcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (cpu != null) cpuId = cpu["ProcessorId"]?.ToString() ?? string.Empty;
            }

            string diskId = string.Empty;
            using (ManagementObjectSearcher diskSearcher = new("SELECT SerialNumber FROM Win32_DiskDrive"))
            {
                ManagementObject? disk = diskSearcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (disk != null) diskId = disk["SerialNumber"]?.ToString() ?? string.Empty;
            }

            int hardwareHash = HashCode.Combine(cpuId, diskId);
            return hardwareHash.ToString("X");
        }
    }

    public record ActivationRequest(string LicenseKey, string HardwareId);

    public record ActivationResponse(bool Success, string Message);
}
