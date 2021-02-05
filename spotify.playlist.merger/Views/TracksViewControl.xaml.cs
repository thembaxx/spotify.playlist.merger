using GalaSoft.MvvmLight.Messaging;
using spotify.playlist.merger.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace spotify.playlist.merger.Views
{
    public sealed partial class TracksViewControl : UserControl
    {
        public TracksViewControl()
        {
            this.InitializeComponent();
            Messenger.Default.Register<MessengerHelper>(this, (messenger) =>
            {
                switch (messenger.Action)
                {
                    case MessengerAction.ScrollToItem:
                        if (messenger.Target == TargetView.Tracks)
                        {
                            if (messenger.Item == null && Content.Items != null) messenger.Item = Content.Items.FirstOrDefault();
                            Content.ScrollIntoView(messenger.Item, ScrollIntoViewAlignment.Leading);
                        }
                        break;
                }
            });
        }
    }
}
