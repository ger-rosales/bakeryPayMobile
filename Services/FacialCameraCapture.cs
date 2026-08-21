namespace BakeryPay.Mobile.Services;

public static class FacialCameraCapture
{
    public static async Task<byte[]?> CaptureAsync()
    {
#if ANDROID
        var permission = await Permissions.RequestAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
            throw new InvalidOperationException("Debes permitir el uso de la cámara para registrar el rostro.");

        var activity = Platform.CurrentActivity as MainActivity
            ?? throw new InvalidOperationException("No hay una actividad Android disponible para la cámara.");
        return await activity.CaptureFaceAsync();
#else
        throw new PlatformNotSupportedException("La captura facial solo está disponible en Android.");
#endif
    }
}
