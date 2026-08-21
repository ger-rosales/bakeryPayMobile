using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;

namespace BakeryPay.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int FacialCameraRequestCode = 7412;
    private TaskCompletionSource<byte[]?>? _facialCameraCompletion;

    public Task<byte[]?> CaptureFaceAsync()
    {
        if (_facialCameraCompletion is not null)
            throw new InvalidOperationException("Ya hay una captura facial en curso.");

        _facialCameraCompletion = new TaskCompletionSource<byte[]?>();
        StartActivityForResult(new Intent(Android.Provider.MediaStore.ActionImageCapture), FacialCameraRequestCode);
        return _facialCameraCompletion.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode != FacialCameraRequestCode || _facialCameraCompletion is null) return;

        var completion = _facialCameraCompletion;
        _facialCameraCompletion = null;
        if (resultCode != Result.Ok || data?.Extras?.Get("data") is not Bitmap bitmap)
        {
            completion.TrySetResult(null);
            return;
        }

        using var stream = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Jpeg, 95, stream);
        completion.TrySetResult(stream.ToArray());
    }
}
