namespace App
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnAktualizacja = new Button();
            SuspendLayout();
            // 
            // btnAktualizacja
            // 
            btnAktualizacja.Font = new Font("Segoe UI", 36F, FontStyle.Bold, GraphicsUnit.Point, 238);
            btnAktualizacja.Location = new Point(207, 101);
            btnAktualizacja.Name = "btnAktualizacja";
            btnAktualizacja.Size = new Size(368, 239);
            btnAktualizacja.TabIndex = 0;
            btnAktualizacja.Text = "Aktualizuj";
            btnAktualizacja.UseVisualStyleBackColor = true;
            btnAktualizacja.Click += btnAktualizacja_Click;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnAktualizacja);
            Name = "MainWindow";
            Text = "MainWindow";
            ResumeLayout(false);
        }

        #endregion

        private Button btnAktualizacja;
    }
}