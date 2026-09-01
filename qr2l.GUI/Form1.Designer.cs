namespace qr2l.GUI;

sealed partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        mainGrid = new System.Windows.Forms.TableLayoutPanel();
        toolBar = new System.Windows.Forms.FlowLayoutPanel();
        saveButton = new System.Windows.Forms.Button();
        copyAsImageButton = new System.Windows.Forms.Button();
        copyAsSvgButton = new System.Windows.Forms.Button();
        spacer = new System.Windows.Forms.Panel();
        donateButton = new System.Windows.Forms.Button();
        helpButton = new System.Windows.Forms.Button();
        languageSelector = new System.Windows.Forms.ComboBox();
        optionsGrid = new System.Windows.Forms.TableLayoutPanel();
        panelFg = new System.Windows.Forms.Panel();
        panelBg = new System.Windows.Forms.Panel();
        logoPath = new System.Windows.Forms.Label();
        textQrCode = new System.Windows.Forms.TextBox();
        pictureQrCode = new System.Windows.Forms.PictureBox();
        imageMenu = new System.Windows.Forms.ContextMenuStrip(components);
        saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        copyAsImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        copyAsSVGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        bottomGrid = new System.Windows.Forms.TableLayoutPanel();
        detectedMode = new System.Windows.Forms.Label();
        bottomRightPanel = new System.Windows.Forms.FlowLayoutPanel();
        buttonRepo = new System.Windows.Forms.Button();
        colorDialog = new System.Windows.Forms.ColorDialog();
        openFileDialog = new System.Windows.Forms.OpenFileDialog();
        saveAsDialog = new System.Windows.Forms.SaveFileDialog();
        tooltip = new System.Windows.Forms.ToolTip(components);
        mainGrid.SuspendLayout();
        toolBar.SuspendLayout();
        optionsGrid.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pictureQrCode).BeginInit();
        imageMenu.SuspendLayout();
        bottomGrid.SuspendLayout();
        bottomRightPanel.SuspendLayout();
        SuspendLayout();
        // 
        // mainGrid
        // 
        mainGrid.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
        mainGrid.ColumnCount = 1;
        mainGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        mainGrid.Controls.Add(toolBar, 0, 0);
        mainGrid.Controls.Add(optionsGrid, 0, 1);
        mainGrid.Controls.Add(textQrCode, 0, 2);
        mainGrid.Controls.Add(pictureQrCode, 0, 3);
        mainGrid.Controls.Add(bottomGrid, 0, 4);
        mainGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        mainGrid.Location = new System.Drawing.Point(0, 0);
        mainGrid.Margin = new System.Windows.Forms.Padding(0);
        mainGrid.Name = "mainGrid";
        mainGrid.RowCount = 5;
        mainGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
        mainGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        mainGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        mainGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 367F));
        mainGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        mainGrid.Size = new System.Drawing.Size(369, 555);
        mainGrid.TabIndex = 0;
        mainGrid.UseWaitCursor = true;
        // 
        // toolBar
        // 
        toolBar.Controls.Add(saveButton);
        toolBar.Controls.Add(copyAsImageButton);
        toolBar.Controls.Add(copyAsSvgButton);
        toolBar.Controls.Add(spacer);
        toolBar.Controls.Add(languageSelector);
        toolBar.Controls.Add(donateButton);
        toolBar.Controls.Add(helpButton);
        toolBar.Dock = System.Windows.Forms.DockStyle.Fill;
        toolBar.Location = new System.Drawing.Point(4, 4);
        toolBar.Name = "toolBar";
        toolBar.Size = new System.Drawing.Size(361, 29);
        toolBar.TabIndex = 4;
        toolBar.UseWaitCursor = true;
        // 
        // saveButton
        // 
        saveButton.Image = global::qr2l.GUI.Properties.Resources.save;
        saveButton.Location = new System.Drawing.Point(3, 3);
        saveButton.Name = "saveButton";
        saveButton.Size = new System.Drawing.Size(24, 24);
        saveButton.TabIndex = 0;
        tooltip.SetToolTip(saveButton, "Save the QRCode");
        saveButton.UseVisualStyleBackColor = true;
        saveButton.UseWaitCursor = true;
        saveButton.Click += saveButton_Click;
        // 
        // copyAsImageButton
        // 
        copyAsImageButton.Image = global::qr2l.GUI.Properties.Resources.clipboard;
        copyAsImageButton.Location = new System.Drawing.Point(33, 3);
        copyAsImageButton.Name = "copyAsImageButton";
        copyAsImageButton.Size = new System.Drawing.Size(24, 24);
        copyAsImageButton.TabIndex = 1;
        tooltip.SetToolTip(copyAsImageButton, "Copy Image to clipboard");
        copyAsImageButton.UseVisualStyleBackColor = true;
        copyAsImageButton.UseWaitCursor = true;
        copyAsImageButton.Click += copyAsImageButton_Click;
        // 
        // copyAsSvgButton
        // 
        copyAsSvgButton.Image = global::qr2l.GUI.Properties.Resources.puzzle;
        copyAsSvgButton.Location = new System.Drawing.Point(63, 3);
        copyAsSvgButton.Name = "copyAsSvgButton";
        copyAsSvgButton.Size = new System.Drawing.Size(24, 24);
        copyAsSvgButton.TabIndex = 2;
        tooltip.SetToolTip(copyAsSvgButton, "Copy SVG string to clipboard");
        copyAsSvgButton.UseVisualStyleBackColor = true;
        copyAsSvgButton.UseWaitCursor = true;
        copyAsSvgButton.Click += copyAsSvgButton_Click;
        // 
        // spacer
        // 
        spacer.Location = new System.Drawing.Point(93, 3);
        spacer.Name = "spacer";
        spacer.Size = new System.Drawing.Size(100, 24);
        spacer.TabIndex = 6;
        spacer.UseWaitCursor = true;
        // 
        // donateButton
        // 
        donateButton.Image = global::qr2l.GUI.Properties.Resources.heart;
        donateButton.Location = new System.Drawing.Point(304, 3);
        donateButton.Name = "donateButton";
        donateButton.Size = new System.Drawing.Size(24, 24);
        donateButton.TabIndex = 3;
        tooltip.SetToolTip(donateButton, "Buy me a coffee");
        donateButton.UseVisualStyleBackColor = true;
        donateButton.UseWaitCursor = true;
        donateButton.Click += donateButton_Click;
        // 
        // helpButton
        // 
        helpButton.Image = global::qr2l.GUI.Properties.Resources.help;
        helpButton.Location = new System.Drawing.Point(334, 3);
        helpButton.Name = "helpButton";
        helpButton.Size = new System.Drawing.Size(24, 24);
        helpButton.TabIndex = 5;
        tooltip.SetToolTip(helpButton, "Help");
        helpButton.UseVisualStyleBackColor = true;
        helpButton.UseWaitCursor = true;
        helpButton.Click += helpButton_Click;
        // 
        // languageSelector
        // 
        languageSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        languageSelector.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        languageSelector.FormattingEnabled = true;
        languageSelector.Location = new System.Drawing.Point(219, 3);
        languageSelector.Name = "languageSelector";
        languageSelector.Size = new System.Drawing.Size(79, 23);
        languageSelector.TabIndex = 7;
        languageSelector.UseWaitCursor = true;
        languageSelector.SelectedIndexChanged += languageSelector_SelectedIndexChanged;
        // 
        // optionsGrid
        // 
        optionsGrid.AutoSize = true;
        optionsGrid.ColumnCount = 3;
        optionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
        optionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
        optionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        optionsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
        optionsGrid.Controls.Add(panelFg, 0, 0);
        optionsGrid.Controls.Add(panelBg, 1, 0);
        optionsGrid.Controls.Add(logoPath, 2, 0);
        optionsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        optionsGrid.Location = new System.Drawing.Point(1, 37);
        optionsGrid.Margin = new System.Windows.Forms.Padding(0);
        optionsGrid.Name = "optionsGrid";
        optionsGrid.RowCount = 1;
        optionsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        optionsGrid.Size = new System.Drawing.Size(367, 29);
        optionsGrid.TabIndex = 3;
        optionsGrid.UseWaitCursor = true;
        // 
        // panelFg
        // 
        panelFg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        panelFg.Cursor = System.Windows.Forms.Cursors.WaitCursor;
        panelFg.Dock = System.Windows.Forms.DockStyle.Fill;
        panelFg.Location = new System.Drawing.Point(3, 3);
        panelFg.Name = "panelFg";
        panelFg.Size = new System.Drawing.Size(85, 23);
        panelFg.TabIndex = 1;
        tooltip.SetToolTip(panelFg, "Sets the pixel color");
        panelFg.UseWaitCursor = true;
        panelFg.Click += panelFg_Click;
        // 
        // panelBg
        // 
        panelBg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        panelBg.Cursor = System.Windows.Forms.Cursors.WaitCursor;
        panelBg.Dock = System.Windows.Forms.DockStyle.Fill;
        panelBg.Location = new System.Drawing.Point(94, 3);
        panelBg.Name = "panelBg";
        panelBg.Size = new System.Drawing.Size(85, 23);
        panelBg.TabIndex = 0;
        tooltip.SetToolTip(panelBg, "Sets the background color");
        panelBg.UseWaitCursor = true;
        panelBg.Click += panelBg_Click;
        // 
        // logoPath
        // 
        logoPath.AutoSize = true;
        logoPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        logoPath.Dock = System.Windows.Forms.DockStyle.Fill;
        logoPath.Location = new System.Drawing.Point(185, 3);
        logoPath.Margin = new System.Windows.Forms.Padding(3);
        logoPath.Name = "logoPath";
        logoPath.Size = new System.Drawing.Size(179, 23);
        logoPath.TabIndex = 2;
        logoPath.Text = "Insert a logo";
        logoPath.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        logoPath.UseWaitCursor = true;
        logoPath.Click += logoPath_Click;
        // 
        // textQrCode
        // 
        textQrCode.AllowDrop = true;
        textQrCode.Dock = System.Windows.Forms.DockStyle.Fill;
        textQrCode.Location = new System.Drawing.Point(1, 67);
        textQrCode.Margin = new System.Windows.Forms.Padding(0);
        textQrCode.Multiline = true;
        textQrCode.Name = "textQrCode";
        textQrCode.PlaceholderText = "Insert here the text for the QR Code";
        textQrCode.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        textQrCode.Size = new System.Drawing.Size(367, 98);
        textQrCode.TabIndex = 0;
        tooltip.SetToolTip(textQrCode, "Insert here the text for the QR Code");
        textQrCode.UseWaitCursor = true;
        textQrCode.TextChanged += OnTextQrCodeChanged;
        // 
        // pictureQrCode
        // 
        pictureQrCode.Anchor = System.Windows.Forms.AnchorStyles.None;
        pictureQrCode.ContextMenuStrip = imageMenu;
        pictureQrCode.Location = new System.Drawing.Point(1, 166);
        pictureQrCode.Margin = new System.Windows.Forms.Padding(0);
        pictureQrCode.Name = "pictureQrCode";
        pictureQrCode.Size = new System.Drawing.Size(367, 367);
        pictureQrCode.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        pictureQrCode.TabIndex = 1;
        pictureQrCode.TabStop = false;
        tooltip.SetToolTip(pictureQrCode, "Right click the image for options");
        pictureQrCode.UseWaitCursor = true;
        // 
        // imageMenu
        // 
        imageMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { saveAsToolStripMenuItem, copyAsImageToolStripMenuItem, copyAsSVGToolStripMenuItem });
        imageMenu.Name = "imageMenu";
        imageMenu.Size = new System.Drawing.Size(155, 70);
        imageMenu.ItemClicked += imageMenu_ItemClicked;
        // 
        // saveAsToolStripMenuItem
        // 
        saveAsToolStripMenuItem.Image = global::qr2l.GUI.Properties.Resources.save;
        saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
        saveAsToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
        saveAsToolStripMenuItem.Text = "Save As";
        // 
        // copyAsImageToolStripMenuItem
        // 
        copyAsImageToolStripMenuItem.Image = global::qr2l.GUI.Properties.Resources.clipboard;
        copyAsImageToolStripMenuItem.Name = "copyAsImageToolStripMenuItem";
        copyAsImageToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
        copyAsImageToolStripMenuItem.Text = "Copy As Image";
        // 
        // copyAsSVGToolStripMenuItem
        // 
        copyAsSVGToolStripMenuItem.Image = global::qr2l.GUI.Properties.Resources.puzzle;
        copyAsSVGToolStripMenuItem.Name = "copyAsSVGToolStripMenuItem";
        copyAsSVGToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
        copyAsSVGToolStripMenuItem.Text = "Copy As SVG";
        // 
        // bottomGrid
        // 
        bottomGrid.ColumnCount = 2;
        bottomGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        bottomGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        bottomGrid.Controls.Add(detectedMode, 0, 0);
        bottomGrid.Controls.Add(bottomRightPanel, 1, 0);
        bottomGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        bottomGrid.Location = new System.Drawing.Point(1, 534);
        bottomGrid.Margin = new System.Windows.Forms.Padding(0);
        bottomGrid.Name = "bottomGrid";
        bottomGrid.Padding = new System.Windows.Forms.Padding(2, 0, 4, 0);
        bottomGrid.RowCount = 1;
        bottomGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        bottomGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
        bottomGrid.Size = new System.Drawing.Size(367, 21);
        bottomGrid.TabIndex = 5;
        bottomGrid.UseWaitCursor = true;
        // 
        // detectedMode
        // 
        detectedMode.Dock = System.Windows.Forms.DockStyle.Fill;
        detectedMode.Location = new System.Drawing.Point(5, 0);
        detectedMode.Name = "detectedMode";
        detectedMode.Size = new System.Drawing.Size(174, 21);
        detectedMode.TabIndex = 0;
        detectedMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        detectedMode.UseWaitCursor = true;
        // 
        // bottomRightPanel
        // 
        bottomRightPanel.AutoSize = true;
        bottomRightPanel.Controls.Add(buttonRepo);
        bottomRightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        bottomRightPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        bottomRightPanel.Location = new System.Drawing.Point(182, 0);
        bottomRightPanel.Margin = new System.Windows.Forms.Padding(0);
        bottomRightPanel.Name = "bottomRightPanel";
        bottomRightPanel.Size = new System.Drawing.Size(181, 21);
        bottomRightPanel.TabIndex = 1;
        bottomRightPanel.UseWaitCursor = true;
        // 
        // buttonRepo
        // 
        buttonRepo.BackColor = System.Drawing.Color.Transparent;
        buttonRepo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
        buttonRepo.FlatAppearance.BorderSize = 0;
        buttonRepo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        buttonRepo.ForeColor = System.Drawing.Color.Transparent;
        buttonRepo.Image = global::qr2l.GUI.Properties.Resources.git;
        buttonRepo.Location = new System.Drawing.Point(163, 0);
        buttonRepo.Margin = new System.Windows.Forms.Padding(0);
        buttonRepo.Name = "buttonRepo";
        buttonRepo.Padding = new System.Windows.Forms.Padding(1);
        buttonRepo.Size = new System.Drawing.Size(18, 18);
        buttonRepo.TabIndex = 0;
        buttonRepo.UseVisualStyleBackColor = false;
        buttonRepo.UseWaitCursor = true;
        buttonRepo.Click += buttonRepo_Click;
        // 
        // openFileDialog
        // 
        openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
        // 
        // saveAsDialog
        // 
        saveAsDialog.DefaultExt = "png";
        saveAsDialog.Title = "Save As";
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(369, 555);
        Controls.Add(mainGrid);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
        MaximizeBox = false;
        SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
        Text = "qr2l - QR Code Tool";
        mainGrid.ResumeLayout(false);
        mainGrid.PerformLayout();
        toolBar.ResumeLayout(false);
        optionsGrid.ResumeLayout(false);
        optionsGrid.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pictureQrCode).EndInit();
        imageMenu.ResumeLayout(false);
        bottomGrid.ResumeLayout(false);
        bottomGrid.PerformLayout();
        bottomRightPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label logoPath;

    private System.Windows.Forms.Button buttonRepo;

    private System.Windows.Forms.FlowLayoutPanel bottomRightPanel;

    private System.Windows.Forms.Label detectedMode;

    private System.Windows.Forms.TableLayoutPanel bottomGrid;

    private System.Windows.Forms.Panel spacer;

    private System.Windows.Forms.ToolTip tooltip;

    private System.Windows.Forms.Button helpButton;

    private System.Windows.Forms.ComboBox languageSelector;

    private System.Windows.Forms.Button copyAsImageButton;
    private System.Windows.Forms.Button copyAsSvgButton;
    private System.Windows.Forms.Button donateButton;

    private System.Windows.Forms.Button saveButton;

    private System.Windows.Forms.FlowLayoutPanel toolBar;

    private System.Windows.Forms.SaveFileDialog saveAsDialog;

    private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem copyAsImageToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem copyAsSVGToolStripMenuItem;

    private System.Windows.Forms.ContextMenuStrip imageMenu;

    private System.Windows.Forms.Panel panelBg;
    private System.Windows.Forms.Panel panelFg;

    private System.Windows.Forms.OpenFileDialog openFileDialog;

    private System.Windows.Forms.ColorDialog colorDialog;

    private System.Windows.Forms.Label labelBackground;

    private System.Windows.Forms.TableLayoutPanel mainGrid;

    private System.Windows.Forms.TextBox textQrCode;

    private System.Windows.Forms.PictureBox pictureQrCode;

    private System.Windows.Forms.TableLayoutPanel optionsGrid;

    #endregion
}