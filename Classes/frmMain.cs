﻿using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using RePlays.Messages;
using RePlays.Services;
using Squirrel;
using static RePlays.Helpers.Functions;

namespace WinFormsApp {
    public partial class frmMain : Form {
        Microsoft.Web.WebView2.WinForms.WebView2 webView2;
        public frmMain() {
            SettingsService.LoadSettings();
            SettingsService.SaveSettings();
            InitializeComponent();
            InitializeWebView2();
            PurgeTempVideos();
            notifyIcon1.Icon = SystemIcons.Application;
            //CheckForUpdates();
        }

        private void frmMain_Load(object sender, System.EventArgs e) {
            InitializeWebView2();
        }

        private async Task CheckForUpdates() {
            using (var manager = UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays")) {
                await manager.Result.UpdateApp();
            }
        }

        private async void InitializeWebView2() {
            pictureBox1.Size = new Size(pictureBox1.Image.Width, pictureBox1.Image.Height);
            pictureBox1.Location = new Point((pictureBox1.Parent.ClientSize.Width / 2) - (pictureBox1.Image.Width / 2),
                                            (pictureBox1.Parent.ClientSize.Height / 2) - (pictureBox1.Image.Height / 2));
            pictureBox1.Refresh();

            if (webView2 == null || webView2.IsDisposed) {
                webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView2.Dock = DockStyle.Fill;
                webView2.Source = new System.Uri("https://localhost:5001/");
                webView2.CoreWebView2InitializationCompleted += CoreWebView2InitializationCompleted;
                webView2.WebMessageReceived += WebMessageReceivedAsync;
                CoreWebView2EnvironmentOptions environmentOptions = new CoreWebView2EnvironmentOptions() {
                    AdditionalBrowserArguments = "--unlimited-storage"
                };
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, environmentOptions);
                await webView2.EnsureCoreWebView2Async(environment);
            }
        }

        private async void CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e) {
            if (webView2.CoreWebView2 == null) {
                DialogResult result =
                    MessageBox.Show(
                        "Microsoft Edge WebView2 Runtime is required to display interface. Would you like to download and run the installer?",
                        "Missing Microsoft Edge WebView2 Runtime", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes) {
                    Process.Start("https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section");
                }
            }
            await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
            //webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        }

        bool firstTime = true;
        private async void WebMessageReceivedAsync(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            var webMessage = await WebMessage.RecieveMessage(webView2, e.WebMessageAsJson);
            if (!this.Controls.Contains(webView2) && webMessage.message == "Initialize") {
                this.Controls.Add(webView2);
                webView2.BringToFront();
                if (firstTime) {
                    this.Size = new Size(1080, 600);
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    CenterToScreen();
                    firstTime = false;
                }
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            if (!new StackTrace().GetFrames().Any(x => x.GetMethod().Name == "Close")) {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                if (!webView2.IsDisposed) {
                    webView2.Dispose();
                }
            }
        }

        FormWindowState _PreviousWindowState;
        private void frmMain_Resize(object sender, System.EventArgs e) {
            if (this.WindowState != FormWindowState.Minimized)
                _PreviousWindowState = WindowState;

            if (this.WindowState != FormWindowState.Minimized) {
                if (webView2.IsDisposed) {
                    InitializeWebView2();
                }
                this.ShowInTaskbar = true;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, System.EventArgs e) {
            this.Activate();
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = _PreviousWindowState;
        }

        private void exitToolStripMenuItem_Click(object sender, System.EventArgs e) {
            notifyIcon1.Visible = false;
            this.Close();
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, System.EventArgs e) {
            CheckForUpdates();
        }

        bool moving;
        Point offset;
        Point original;
        private void frmMain_MouseDown(object sender, MouseEventArgs e) {
            moving = true;
            this.Capture = true;
            offset = MousePosition;
            original = this.Location;
        }

        private void frmMain_MouseMove(object sender, MouseEventArgs e) {
            if (!moving)
                return;

            int x = original.X + MousePosition.X - offset.X;
            int y = original.Y + MousePosition.Y - offset.Y;

            this.Location = new Point(x, y);
        }

        private void frmMain_MouseUp(object sender, MouseEventArgs e) {
            moving = false;
            this.Capture = false;
        }
    }
}