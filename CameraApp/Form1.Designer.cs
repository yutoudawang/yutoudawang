namespace CameraApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // ── HALCON display windows ────────────────────────────────────────────
        private HalconDotNet.HWindowControl hWindowControl1;
        private HalconDotNet.HWindowControl hWindowControl2;
        private HalconDotNet.HWindowControl hWindowControl3;

        // ── Buttons ───────────────────────────────────────────────────────────
        internal System.Windows.Forms.Button btnStartHW;
        internal System.Windows.Forms.Button btnStartCont;
        internal System.Windows.Forms.Button btnStop;
        private  System.Windows.Forms.Button btnGrabH1;
        private  System.Windows.Forms.Button btnGrabH2;
        private  System.Windows.Forms.Button btnGrabH3;

        // ── Status label ──────────────────────────────────────────────────────
        private System.Windows.Forms.Label lblStatus;

        // ── Layout panels ─────────────────────────────────────────────────────
        private System.Windows.Forms.TableLayoutPanel tlpCameras;
        private System.Windows.Forms.FlowLayoutPanel  flpControls;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ── HALCON windows ────────────────────────────────────────────────
            this.hWindowControl1 = new HalconDotNet.HWindowControl();
            this.hWindowControl2 = new HalconDotNet.HWindowControl();
            this.hWindowControl3 = new HalconDotNet.HWindowControl();

            // ── Buttons ───────────────────────────────────────────────────────
            this.btnStartHW   = new System.Windows.Forms.Button();
            this.btnStartCont = new System.Windows.Forms.Button();
            this.btnStop      = new System.Windows.Forms.Button();
            this.btnGrabH1    = new System.Windows.Forms.Button();
            this.btnGrabH2    = new System.Windows.Forms.Button();
            this.btnGrabH3    = new System.Windows.Forms.Button();

            // ── Status label ──────────────────────────────────────────────────
            this.lblStatus = new System.Windows.Forms.Label();

            // ── Layout panels ─────────────────────────────────────────────────
            this.tlpCameras  = new System.Windows.Forms.TableLayoutPanel();
            this.flpControls = new System.Windows.Forms.FlowLayoutPanel();

            this.SuspendLayout();

            // ── hWindowControl1 (Camera H1 – top-left) ────────────────────────
            this.hWindowControl1.BackColor  = System.Drawing.Color.Black;
            this.hWindowControl1.Dock       = System.Windows.Forms.DockStyle.Fill;
            this.hWindowControl1.ImagePart  = new System.Drawing.Rectangle(0, 0, 640, 480);
            this.hWindowControl1.TabIndex   = 0;
            this.hWindowControl1.Margin     = new System.Windows.Forms.Padding(2);

            // ── hWindowControl2 (Camera H2 – top-centre) ─────────────────────
            this.hWindowControl2.BackColor  = System.Drawing.Color.Black;
            this.hWindowControl2.Dock       = System.Windows.Forms.DockStyle.Fill;
            this.hWindowControl2.ImagePart  = new System.Drawing.Rectangle(0, 0, 640, 480);
            this.hWindowControl2.TabIndex   = 1;
            this.hWindowControl2.Margin     = new System.Windows.Forms.Padding(2);

            // ── hWindowControl3 (Camera H3 – top-right) ──────────────────────
            this.hWindowControl3.BackColor  = System.Drawing.Color.Black;
            this.hWindowControl3.Dock       = System.Windows.Forms.DockStyle.Fill;
            this.hWindowControl3.ImagePart  = new System.Drawing.Rectangle(0, 0, 640, 480);
            this.hWindowControl3.TabIndex   = 2;
            this.hWindowControl3.Margin     = new System.Windows.Forms.Padding(2);

            // ── TableLayoutPanel (3 cameras in a row) ─────────────────────────
            this.tlpCameras.ColumnCount = 3;
            this.tlpCameras.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 33.33f));
            this.tlpCameras.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 33.33f));
            this.tlpCameras.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 33.34f));
            this.tlpCameras.RowCount = 1;
            this.tlpCameras.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 100f));
            this.tlpCameras.Controls.Add(this.hWindowControl1, 0, 0);
            this.tlpCameras.Controls.Add(this.hWindowControl2, 1, 0);
            this.tlpCameras.Controls.Add(this.hWindowControl3, 2, 0);
            this.tlpCameras.Dock    = System.Windows.Forms.DockStyle.Fill;
            this.tlpCameras.Margin  = new System.Windows.Forms.Padding(0);

            // ── Acquisition mode buttons ──────────────────────────────────────
            this.btnStartHW.Text    = "Start Hardware Trigger";
            this.btnStartHW.Size    = new System.Drawing.Size(160, 28);
            this.btnStartHW.Click  += this.btnStartHW_Click;

            this.btnStartCont.Text   = "Start Continuous";
            this.btnStartCont.Size   = new System.Drawing.Size(130, 28);
            this.btnStartCont.Click += this.btnStartCont_Click;

            this.btnStop.Text    = "Stop";
            this.btnStop.Size    = new System.Drawing.Size(70, 28);
            this.btnStop.Click  += this.btnStop_Click;
            this.btnStop.Enabled = false;

            // ── Manual grab buttons ───────────────────────────────────────────
            this.btnGrabH1.Text    = "Grab H1";
            this.btnGrabH1.Size    = new System.Drawing.Size(80, 28);
            this.btnGrabH1.Click  += this.btnGrabH1_Click;

            this.btnGrabH2.Text    = "Grab H2";
            this.btnGrabH2.Size    = new System.Drawing.Size(80, 28);
            this.btnGrabH2.Click  += this.btnGrabH2_Click;

            this.btnGrabH3.Text    = "Grab H3";
            this.btnGrabH3.Size    = new System.Drawing.Size(80, 28);
            this.btnGrabH3.Click  += this.btnGrabH3_Click;

            // ── Status label ──────────────────────────────────────────────────
            this.lblStatus.AutoSize   = true;
            this.lblStatus.Text       = "Initialising…";
            this.lblStatus.Margin     = new System.Windows.Forms.Padding(8, 6, 0, 0);

            // ── FlowLayoutPanel (control strip at the bottom) ─────────────────
            this.flpControls.AutoSize        = true;
            this.flpControls.AutoSizeMode    = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpControls.Dock            = System.Windows.Forms.DockStyle.Bottom;
            this.flpControls.FlowDirection   = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpControls.Padding         = new System.Windows.Forms.Padding(4);
            this.flpControls.WrapContents    = false;
            this.flpControls.Controls.Add(this.btnStartHW);
            this.flpControls.Controls.Add(this.btnStartCont);
            this.flpControls.Controls.Add(this.btnStop);
            this.flpControls.Controls.Add(this.btnGrabH1);
            this.flpControls.Controls.Add(this.btnGrabH2);
            this.flpControls.Controls.Add(this.btnGrabH3);
            this.flpControls.Controls.Add(this.lblStatus);

            // ── Form ──────────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1440, 560);
            this.Controls.Add(this.tlpCameras);
            this.Controls.Add(this.flpControls);
            this.MinimumSize         = new System.Drawing.Size(800, 400);
            this.Text                = "Hikvision MVS – Hardware Trigger Demo";
            this.Load               += this.Form1_Load;
            this.FormClosing        += this.Form1_FormClosing;

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
