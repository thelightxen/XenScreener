using System.ComponentModel;
using System.Reflection;

namespace XenScreener;

partial class About
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
        Logo = new System.Windows.Forms.PictureBox();
        TitleProgram = new System.Windows.Forms.Label();
        Description = new System.Windows.Forms.Label();
        Info = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)Logo).BeginInit();
        SuspendLayout();
        // 
        // Logo
        // 
        Logo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        Logo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
        Logo.Image = ((System.Drawing.Image)resources.GetObject("Logo.Image"));
        Logo.InitialImage = ((System.Drawing.Image)resources.GetObject("Logo.InitialImage"));
        Logo.Location = new System.Drawing.Point(0, 19);
        Logo.Margin = new System.Windows.Forms.Padding(0);
        Logo.Name = "Logo";
        Logo.Size = new System.Drawing.Size(484, 256);
        Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        Logo.TabIndex = 0;
        Logo.TabStop = false;
        // 
        // TitleProgram
        // 
        TitleProgram.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        TitleProgram.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)204));
        TitleProgram.Location = new System.Drawing.Point(12, 286);
        TitleProgram.Name = "TitleProgram";
        TitleProgram.Size = new System.Drawing.Size(460, 29);
        TitleProgram.TabIndex = 1;
        TitleProgram.Text = "XenScreener v";
        TitleProgram.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // Description
        // 
        Description.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        Description.Font = new System.Drawing.Font("Calibri", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)204));
        Description.Location = new System.Drawing.Point(0, 325);
        Description.Name = "Description";
        Description.Padding = new System.Windows.Forms.Padding(25, 0, 25, 0);
        Description.Size = new System.Drawing.Size(484, 59);
        Description.TabIndex = 2;
        Description.Text = "Makes a screenshot of the monitor depending on the cursor location";
        Description.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // Info
        // 
        Info.AllowDrop = true;
        Info.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        Info.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)204));
        Info.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
        Info.Location = new System.Drawing.Point(0, 396);
        Info.Name = "Info";
        Info.Size = new System.Drawing.Size(484, 74);
        Info.TabIndex = 3;
        Info.Text = "Coded by XenFFly\r\n\r\n.NET 8.0";
        Info.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // About
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(484, 491);
        Controls.Add(Info);
        Controls.Add(Description);
        Controls.Add(TitleProgram);
        Controls.Add(Logo);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        HelpButton = true;
        Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
        ImeMode = System.Windows.Forms.ImeMode.Hiragana;
        MaximizeBox = false;
        MaximumSize = new System.Drawing.Size(500, 530);
        MdiChildrenMinimizedAnchorBottom = false;
        MinimumSize = new System.Drawing.Size(500, 530);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "About";
        Load += About_Load;
        ((System.ComponentModel.ISupportInitialize)Logo).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label Info;

    private System.Windows.Forms.Label TitleProgram;

    private System.Windows.Forms.Label Description;

    private System.Windows.Forms.PictureBox Logo;

    #endregion
}