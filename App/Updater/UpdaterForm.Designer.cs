using System.Drawing;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Core.Updater;

namespace App.Updater
{
    partial class UpdaterForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel pnlTop;
        private Panel pnlBottom;
        private Label lblStatus;
        private Label lblCount;
        private RichTextBox rtbOutput;
        private Button btnCheck;
        private Button btnUpdate;
        private Button btnCancel;
        private Button btnRaw;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlTop = new Panel();
            pnlBottom = new Panel();
            lblStatus = new Label();
            lblCount = new Label();
            rtbOutput = new RichTextBox();
            btnCheck = new Button();
            btnUpdate = new Button();
            btnCancel = new Button();
            btnRaw = new Button();

            SuspendLayout();

            // ── Form ─────────────────────────────────────────────────
            Text = "Aktualizator — Winget";
            Size = new Size(880, 540);
            MinimumSize = new Size(600, 400);
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            Font = new System.Drawing.Font("Segoe UI", 9f);

            // ── pnlTop ───────────────────────────────────────────────
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 64;
            pnlTop.Padding = new Padding(10, 8, 10, 0);
            pnlTop.BackColor = Color.FromArgb(40, 40, 40);

            lblStatus.AutoSize = true;
            lblStatus.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            lblStatus.ForeColor = Color.White;
            lblStatus.Text = "Gotowy";
            lblStatus.Location = new Point(10, 8);

            lblCount.AutoSize = true;
            lblCount.ForeColor = Color.LightGray;
            lblCount.Text = "Aplikacji do aktualizacji: —";
            lblCount.Location = new Point(10, 34);

            pnlTop.Controls.Add(lblStatus);
            pnlTop.Controls.Add(lblCount);

            // ── rtbOutput ────────────────────────────────────────────
            rtbOutput.Dock = DockStyle.Fill;
            rtbOutput.BackColor = Color.FromArgb(18, 18, 18);
            rtbOutput.ForeColor = Color.White;
            rtbOutput.Font = new System.Drawing.Font("Consolas", 9f);
            rtbOutput.ReadOnly = true;
            rtbOutput.BorderStyle = BorderStyle.None;
            rtbOutput.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbOutput.Padding = new Padding(6);
            rtbOutput.WordWrap = false;

            // ── pnlBottom ────────────────────────────────────────────
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 52;
            pnlBottom.Padding = new Padding(10, 8, 10, 8);
            pnlBottom.BackColor = Color.FromArgb(40, 40, 40);
            pnlBottom.MouseMove += PnlBottom_MouseMove;

            btnCheck = new Button
            {
                Text = "Sprawdź ponownie",
                Width = 160,
                Height = 34,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnUpdate = new Button
            {
                Text = "Uruchom aktualizacje",
                Width = 160,
                Height = 34,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnCancel = new Button
            {
                Text = "Anuluj",
                Width = 160,
                Height = 34,
                BackColor = Color.FromArgb(180, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnRaw = new Button
            {
                Text = "Czysty",
                Width = 160,
                Height = 34,
                BackColor = Color.LightGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnCheck.Enabled = false;
            btnUpdate.Enabled = false;
            btnCancel.Enabled = false;
            btnRaw.Enabled = false;

            btnCheck.Location = new Point(10, 9);
            btnUpdate.Location = new Point(180, 9);
            btnCancel.Location = new Point(350, 9);
            btnRaw.Location = new Point(520, 9);

            btnCheck.Click += BtnCheck_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnCancel.Click += BtnCancel_Click;
            btnRaw.Click += BtnRaw_Click;

            pnlBottom.Controls.AddRange(new Control[] { btnCheck, btnUpdate, btnCancel, btnRaw });

            // ── Składanie ────────────────────────────────────────────
            Controls.Add(rtbOutput);
            Controls.Add(pnlTop);
            Controls.Add(pnlBottom);

            ResumeLayout(false);
        }
    }
}