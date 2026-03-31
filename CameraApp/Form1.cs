using System;
using System.Windows.Forms;
using HalconDotNet;

namespace CameraApp
{
    /// <summary>
    /// Main application form.
    /// Hosts three HALCON display windows (one per camera) and demonstrates
    /// both hardware-trigger and continuous acquisition using <see cref="HikCam"/>.
    /// </summary>
    public partial class Form1 : Form
    {
        // ── Camera instances ──────────────────────────────────────────────────
        private HikCam _camH1;
        private HikCam _camH2;
        private HikCam _camH3;

        // ── Stored images (last grabbed frame, one per camera) ────────────────
        // Named to match the convention referenced in the problem statement.
        private HObject camH1_ho_Image;
        private HObject camH2_ho_Image;
        private HObject camH3_ho_Image;

        // ─────────────────────────────────────────────────────────────────────

        public Form1()
        {
            InitializeComponent();
        }

        // ── Form lifecycle ────────────────────────────────────────────────────

        private void Form1_Load(object sender, EventArgs e)
        {
            _camH1 = new HikCam();
            _camH2 = new HikCam();
            _camH3 = new HikCam();

            bool h1 = _camH1.OpenCam(0);
            bool h2 = _camH2.OpenCam(1);
            bool h3 = _camH3.OpenCam(2);

            lblStatus.Text =
                $"H1: {(h1 ? "OK" : "OFFLINE")}   " +
                $"H2: {(h2 ? "OK" : "OFFLINE")}   " +
                $"H3: {(h3 ? "OK" : "OFFLINE")}";

            btnStop.Enabled = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Ensure clean shutdown: stop acquisition then dispose all cameras.
            StopAcquisition();

            _camH1?.Dispose();
            _camH2?.Dispose();
            _camH3?.Dispose();

            camH1_ho_Image?.Dispose();
            camH2_ho_Image?.Dispose();
            camH3_ho_Image?.Dispose();
        }

        // ── Hardware-trigger acquisition ──────────────────────────────────────

        /// <summary>
        /// Configure each camera for hardware (line) trigger and start
        /// callback-based grabbing.
        /// Wire an external trigger signal to each camera's Line0 input.
        /// </summary>
        public void StartHardwareTriggerAcquisition()
        {
            AttachAndStart(_camH1, TriggerMode.Hardware, OnCamH1Frame);
            AttachAndStart(_camH2, TriggerMode.Hardware, OnCamH2Frame);
            AttachAndStart(_camH3, TriggerMode.Hardware, OnCamH3Frame);

            btnStartHW.Enabled  = false;
            btnStartCont.Enabled = false;
            btnStop.Enabled     = true;
        }

        /// <summary>
        /// Switch to continuous (free-run) mode and start callback-based grabbing.
        /// No external signal is required.
        /// </summary>
        public void StartContinuousAcquisition()
        {
            AttachAndStart(_camH1, TriggerMode.Continuous, OnCamH1Frame);
            AttachAndStart(_camH2, TriggerMode.Continuous, OnCamH2Frame);
            AttachAndStart(_camH3, TriggerMode.Continuous, OnCamH3Frame);

            btnStartHW.Enabled   = false;
            btnStartCont.Enabled = false;
            btnStop.Enabled      = true;
        }

        /// <summary>Stop acquisition on all cameras.</summary>
        public void StopAcquisition()
        {
            _camH1?.StopGrabbing();
            _camH2?.StopGrabbing();
            _camH3?.StopGrabbing();

            if (!IsDisposed)
            {
                btnStartHW.Enabled   = true;
                btnStartCont.Enabled = true;
                btnStop.Enabled      = false;
            }
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void btnStartHW_Click(object sender, EventArgs e)    => StartHardwareTriggerAcquisition();
        private void btnStartCont_Click(object sender, EventArgs e)  => StartContinuousAcquisition();
        private void btnStop_Click(object sender, EventArgs e)        => StopAcquisition();

        private void btnGrabH1_Click(object sender, EventArgs e) =>
            GrabManual(_camH1, hWindowControl1.HalconWindow, ref camH1_ho_Image);
        private void btnGrabH2_Click(object sender, EventArgs e) =>
            GrabManual(_camH2, hWindowControl2.HalconWindow, ref camH2_ho_Image);
        private void btnGrabH3_Click(object sender, EventArgs e) =>
            GrabManual(_camH3, hWindowControl3.HalconWindow, ref camH3_ho_Image);

        // ── Frame callbacks (fired on SDK thread) ─────────────────────────────

        private void OnCamH1Frame(object sender, FrameEventArgs e) =>
            DisplayFrame(e, hWindowControl1.HalconWindow,
                         () => camH1_ho_Image,
                         img  => camH1_ho_Image = img);

        private void OnCamH2Frame(object sender, FrameEventArgs e) =>
            DisplayFrame(e, hWindowControl2.HalconWindow,
                         () => camH2_ho_Image,
                         img  => camH2_ho_Image = img);

        private void OnCamH3Frame(object sender, FrameEventArgs e) =>
            DisplayFrame(e, hWindowControl3.HalconWindow,
                         () => camH3_ho_Image,
                         img  => camH3_ho_Image = img);

        // ── Core helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Configure trigger mode, subscribe to the callback (once), and start grabbing.
        /// </summary>
        private static void AttachAndStart(
            HikCam cam,
            TriggerMode mode,
            EventHandler<FrameEventArgs> handler)
        {
            if (cam == null) return;
            // Remove before re-adding to prevent duplicate subscriptions on restart.
            cam.FrameReceived -= handler;
            cam.FrameReceived += handler;
            cam.ConfigureTrigger(mode);
            cam.StartGrabbing();
        }

        /// <summary>
        /// Build a HALCON image from the frame bytes, then marshal display to
        /// the UI thread.
        /// </summary>
        private void DisplayFrame(
            FrameEventArgs e,
            HWindow window,
            Func<HObject>  getStored,
            Action<HObject> setStored)
        {
            // Build image on the callback thread (no UI access here).
            HObject newImage = HikCam.BuildHalconImageFromBytes(
                e.Data, e.Width, e.Height, e.PixelType);

            if (newImage == null) return;

            // Switch to the UI thread for display.
            Action uiAction = () =>
            {
                getStored()?.Dispose();
                setStored(newImage);
                Tools.SetupWindow(window, e);
                Tools.DispObj(newImage, window);
            };

            if (InvokeRequired)
                BeginInvoke(uiAction);
            else
                uiAction();
        }

        /// <summary>
        /// Manual (polling) grab: one frame, blocking, stored and displayed immediately.
        /// </summary>
        private void GrabManual(HikCam cam, HWindow window, ref HObject stored)
        {
            if (cam == null) return;
            if (!cam.GrabImage()) return;

            stored?.Dispose();
            stored = cam.ho_Image;

            if (stored != null)
            {
                try
                {
                    HTuple imgWidth, imgHeight;
                    HOperatorSet.GetImageSize(stored, out imgWidth, out imgHeight);
                    Tools.SetupWindow(window, imgWidth.I, imgHeight.I);
                }
                catch (HalconException)
                {
                    Tools.SetupWindow(window, 0, 0);
                }
                Tools.DispObj(stored, window);
            }
        }
    }
}
