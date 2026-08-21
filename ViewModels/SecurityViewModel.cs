using BakeryPay.Mobile.Services;
using BakeryPay.Mobile.Services.Biometric;

namespace BakeryPay.Mobile.ViewModels;

public class SecurityViewModel : BaseViewModel
{
    private readonly AuthApiService _authApiService;
    private readonly SessionStorageService _sessionStorageService;
    private readonly DeviceInstallationService _deviceInstallationService;
    private readonly IBiometricService _biometricService;
    private string _email = string.Empty;
    private string _fullName = string.Empty;
    private string _message = string.Empty;
    private string _biometricStatus = "Biometria no registrada";
    private bool _isBiometricRegistered;
    private string _facialActionText = "Registrar reconocimiento facial";

    public SecurityViewModel(
        AuthApiService authApiService,
        SessionStorageService sessionStorageService,
        DeviceInstallationService deviceInstallationService,
        IBiometricService biometricService)
    {
        _authApiService = authApiService;
        _sessionStorageService = sessionStorageService;
        _deviceInstallationService = deviceInstallationService;
        _biometricService = biometricService;

        Title = "Seguridad";
        RegisterDeviceBiometricCommand = new AsyncCommand(RegisterDeviceBiometricAsync);
        RegisterBiometricCommand = new AsyncCommand(RegisterBiometricAsync);
        OpenChangePasswordCommand = new AsyncCommand(OpenChangePasswordAsync);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string BiometricStatus
    {
        get => _biometricStatus;
        set => SetProperty(ref _biometricStatus, value);
    }

    public bool IsBiometricRegistered
    {
        get => _isBiometricRegistered;
        set => SetProperty(ref _isBiometricRegistered, value);
    }

    public string FacialActionText
    {
        get => _facialActionText;
        set => SetProperty(ref _facialActionText, value);
    }

    public AsyncCommand RegisterDeviceBiometricCommand { get; }
    public AsyncCommand RegisterBiometricCommand { get; }
    public AsyncCommand OpenChangePasswordCommand { get; }

    public async Task LoadAsync()
    {
        var session = await _sessionStorageService.GetSessionAsync();
        if (session is null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        Email = session.Email;
        FullName = session.FullName;
        IsBiometricRegistered = session.BiometricEnabled;
        BiometricStatus = session.BiometricEnabled
            ? "Biometria registrada para este dispositivo"
            : "Biometria no registrada";
        FacialActionText = session.BiometricEnabled
            ? "Actualizar reconocimiento facial"
            : "Registrar reconocimiento facial";
    }

    private async Task RegisterBiometricAsync()
    {
        try
        {
            IsBusy = true;
            Message = string.Empty;

            var imageBytes = await FacialCameraCapture.CaptureAsync();
            if (imageBytes is null)
            {
                Message = "Se canceló la captura facial.";
                return;
            }

            var response = await _authApiService.RegisterFacialAsync(Convert.ToBase64String(imageBytes));

            if (response?.Success != true || response.Data is null)
            {
                Message = response?.Message ?? "No fue posible registrar el reconocimiento facial.";
                return;
            }

            await _sessionStorageService.SaveSessionAsync(response.Data);
            IsBiometricRegistered = true;
            FacialActionText = "Actualizar reconocimiento facial";
            BiometricStatus = "Reconocimiento facial registrado correctamente";
            Message = "Reconocimiento facial habilitado para futuros ingresos.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RegisterDeviceBiometricAsync()
    {
        try
        {
            IsBusy = true;
            Message = string.Empty;

            var availability = await _biometricService.GetAvailabilityAsync();
            if (!availability.IsAvailable)
            {
                Message = "No hay una huella fuerte configurada. Registrala primero en los ajustes de Android.";
                return;
            }

            var authentication = await _biometricService.AuthenticateAsync(
                "Registrar huella en BakeryPay",
                "Confirma tu huella para vincular este dispositivo.");
            if (!authentication.Success)
            {
                Message = authentication.Message;
                return;
            }

            var deviceId = await _deviceInstallationService.GetOrCreateDeviceIdAsync();
            var deviceSecret = await _deviceInstallationService.GetOrCreateDeviceSecretAsync();
            var response = await _authApiService.RegisterBiometricAsync(
                deviceId,
                DeviceInfo.Current.Name,
                DeviceInfo.Current.Platform.ToString(),
                deviceSecret,
                (int)Models.BiometricTypeOption.Fingerprint);
            if (response?.Success != true || response.Data is null)
            {
                Message = response?.Message ?? "No fue posible vincular la huella del dispositivo.";
                return;
            }

            await _sessionStorageService.SaveSessionAsync(response.Data);
            IsBiometricRegistered = true;
            BiometricStatus = "Huella del dispositivo registrada correctamente";
            Message = "El sensor confirmo la huella y el dispositivo quedo vinculado.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task OpenChangePasswordAsync() => Shell.Current.GoToAsync("//change-password");
}
