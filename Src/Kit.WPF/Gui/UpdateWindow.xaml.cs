﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Microsoft.HockeyApp.Gui
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private bool _canceled = false;
        private IAppVersion _newVersion = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateWindow"/> class.
        /// </summary>
        /// <param name="newVersion">New Version.</param>
        /// <param name="currentVersion">Current Version.</param>
        public UpdateWindow(IAppVersion newVersion, System.Version currentVersion)
        {
            this.InitializeComponent();
            MouseDown += delegate { DragMove(); };

            this._newVersion = newVersion;
            this.txtTopic.Text = String.Format(LocalizedStrings.LocalizedResources.UpdateHeader, newVersion.Title);
            this.txtText.Text = String.Format(LocalizedStrings.LocalizedResources.UpdateText, newVersion.Title, currentVersion.ToString(), newVersion.Version);
            this.txtReleaseNotes.Text = LocalizedStrings.LocalizedResources.ReleaseNotes;
            if (!string.IsNullOrWhiteSpace(newVersion.Notes))
            {
                this.releaseNotes.NavigateToString(makeUtf8html(newVersion.Notes));
                this.releaseNotes.Navigating += (sender, args) =>
                {
                    Process.Start(args.Uri.ToString());
                    args.Cancel = true;
                };
            }
        }

        /// <summary>
        /// Make WebBrowser encoding to utf-8  
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string makeUtf8html(string text)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'></head>");
            sb.Append("<body>");
            sb.Append(text);
            sb.Append("</body></html>");
            return sb.ToString();
        }
     
        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            this.IsBusyGrid.Visibility = System.Windows.Visibility.Visible;
            this.DownloadProgressBar.Visibility = System.Windows.Visibility.Visible;
            this.releaseNotes.Visibility = System.Windows.Visibility.Collapsed;
            Task t = this._newVersion.DownloadMsi(
                progressInfo =>{
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.DownloadProgressBar.Value = progressInfo.ProgressPercentage;
                    }));
                    return this._canceled;
                },
                () =>
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.Close();
                        if (!this._canceled)
                        {
                            this._newVersion.InstallVersion();
                        }
                    }))
                );
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this._canceled = true;
            this.Close();
        }
    }
}
