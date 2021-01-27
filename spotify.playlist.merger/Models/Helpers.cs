using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace spotify.playlist.merger.Models
{
    public class Helpers
    {
        public static async void DisplayDialog(string title, string message)
        {
            try
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "Close"
                };

                await dialog.ShowAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string CleanString(string str)
        {
            string newStr = Regex.Replace(str, @"<[^>]+>| ", " ").Trim();
            newStr = Regex.Replace(newStr, "&(?!amp;)", "&");
            return newStr;
        }

        public static async Task<StorageFile> ImageFileDialogPicker()
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.Thumbnail,
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary
                };

                picker.FileTypeFilter.Add(".jpg");

                return await picker.PickSingleFileAsync();

            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<string> ImageToBase64(StorageFile file)
        {
            try
            {
                byte[] bytes;
                var buffer = await FileIO.ReadBufferAsync(file);
                using (MemoryStream mstream = new MemoryStream())
                {
                    await buffer.AsStream().CopyToAsync(mstream);
                    bytes = mstream.ToArray();
                }

                return Convert.ToBase64String(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
