using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace spotify.playlist.merger.Models
{
    public class Helpers
    {
        public static async Task DisplayDialog(string title, string message)
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

        public static string MillisecondsToStringAlt(int milliSeconds)
        {
            string result = "0";
            TimeSpan t = TimeSpan.FromMilliseconds(milliSeconds);
            if (t.Hours > 0)
            {
                if (t.Minutes > 0)
                {
                    if (t.Minutes > 9)
                        result = t.Hours + ":" + t.Minutes;
                    else
                        result = t.Hours + ":0" + t.Minutes;
                }
                else
                    result = t.Hours + ":00";
            }
            else if (t.Minutes > 0)
            {
                if (t.Seconds > 0)
                {
                    if (t.Seconds > 9)
                        result = t.Minutes + ":" + t.Seconds;
                    else
                        result = t.Minutes + ":0" + t.Seconds;
                }
                else
                    result = t.Minutes + ":00";
            }
            else if (t.Seconds > 0)
            {
                if (t.Seconds > 9)
                    result = "0:" + t.Seconds; //3s > 0:03, 12s 0:12
                else
                    result = "0:0" + t.Seconds;
            }

            return result;
        }

        #region Playback

        public static async Task<bool> OpenSpotifyAppAsync(string url, string webUrl)
        {
            //"packageFamilyName": "SpotifyAB.SpotifyMusic_zpdnekdrzrea0",
            //"packageIdentityName": "SpotifyAB.SpotifyMusic",
            //"windowsPhoneLegacyId": "caac1b9d-621b-4f96-b143-e10e1397740a",
            //"publisherCertificateName": "CN=453637B3-4E12-4CDF-B0D3-2A3C863BF6EF"
            try
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "No active devices",
                    Content = "There are currently no active devices, open on spotify?",
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Open Spotify App",
                    SecondaryButtonText = "Open Web player"
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var uri = new Uri(url);
                    //Set the recommended app
                    var options = new LauncherOptions
                    {
                        PreferredApplicationPackageFamilyName = "SpotifyAB.SpotifyMusic_zpdnekdrzrea0",
                        PreferredApplicationDisplayName = "Spotify Music"
                    };
                    return await Launcher.LaunchUriAsync(uri, options);
                }
                else if (result == ContentDialogResult.Secondary && !string.IsNullOrEmpty(webUrl))
                {
                    return await Launcher.LaunchUriAsync(new Uri(webUrl));
                }
                return false;
            }
            catch (Exception)
            {
                await DisplayDialog("Error eccured", "Could not open link, please try again");
                return false;
            }
        }

        public static async Task<bool> OpenInSpotifyAppAsync(Uri uri)
        {
            try
            {
                //Set the recommended app
                var options = new LauncherOptions
                {
                    PreferredApplicationPackageFamilyName = "SpotifyAB.SpotifyMusic_zpdnekdrzrea0",
                    PreferredApplicationDisplayName = "Spotify Music"
                };
                return await Launcher.LaunchUriAsync(uri, options);
            }
            catch (Exception)
            {
                await DisplayDialog("Error eccured", "Could not open link, please try again");
                return false;
            }
        }

        #endregion

    }
}
