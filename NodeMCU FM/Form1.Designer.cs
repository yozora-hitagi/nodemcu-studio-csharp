namespace NodeMCU_FM
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.SerialPortComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.BautrateComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.connectButton = new System.Windows.Forms.ToolStripButton();
            this.filelistbox = new System.Windows.Forms.ListBox();
            this.cmdbox = new System.Windows.Forms.TextBox();
            this.resultbox = new System.Windows.Forms.RichTextBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SerialPortComboBox,
            this.toolStripButton1,
            this.BautrateComboBox,
            this.connectButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(523, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // SerialPortComboBox
            // 
            this.SerialPortComboBox.Name = "SerialPortComboBox";
            this.SerialPortComboBox.Size = new System.Drawing.Size(121, 25);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = global::NodeMCU_FM.Properties.Resources.refresh;
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "toolStripButton1";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // BautrateComboBox
            // 
            this.BautrateComboBox.Items.AddRange(new object[] {
            "300",
            "600",
            "1200",
            "2400",
            "4800",
            "9600",
            "19200",
            "38400",
            "57600",
            "74880",
            "115200",
            "230400",
            "256000",
            "460800",
            "921600",
            "1843200",
            "3686400"});
            this.BautrateComboBox.Name = "BautrateComboBox";
            this.BautrateComboBox.Size = new System.Drawing.Size(121, 25);
            this.BautrateComboBox.Text = "115200";
            // 
            // connectButton
            // 
            this.connectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.connectButton.Image = global::NodeMCU_FM.Properties.Resources.disconnected;
            this.connectButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(23, 22);
            this.connectButton.Text = "connectButton";
            this.connectButton.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // filelistbox
            // 
            this.filelistbox.Dock = System.Windows.Forms.DockStyle.Left;
            this.filelistbox.FormattingEnabled = true;
            this.filelistbox.ItemHeight = 12;
            this.filelistbox.Location = new System.Drawing.Point(0, 25);
            this.filelistbox.Name = "filelistbox";
            this.filelistbox.Size = new System.Drawing.Size(150, 431);
            this.filelistbox.TabIndex = 1;
            // 
            // cmdbox
            // 
            this.cmdbox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cmdbox.Location = new System.Drawing.Point(150, 435);
            this.cmdbox.Name = "cmdbox";
            this.cmdbox.Size = new System.Drawing.Size(373, 21);
            this.cmdbox.TabIndex = 2;
            // 
            // resultbox
            // 
            this.resultbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultbox.Location = new System.Drawing.Point(150, 25);
            this.resultbox.Name = "resultbox";
            this.resultbox.Size = new System.Drawing.Size(373, 410);
            this.resultbox.TabIndex = 3;
            this.resultbox.Text = "";
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(150, 25);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 410);
            this.splitter1.TabIndex = 4;
            this.splitter1.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(523, 456);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.resultbox);
            this.Controls.Add(this.cmdbox);
            this.Controls.Add(this.filelistbox);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Form1";
            this.Text = "NodeMCU FM";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripComboBox SerialPortComboBox;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripComboBox BautrateComboBox;
        private System.Windows.Forms.ToolStripButton connectButton;
        private System.Windows.Forms.ListBox filelistbox;
        private System.Windows.Forms.TextBox cmdbox;
        private System.Windows.Forms.RichTextBox resultbox;
        private System.Windows.Forms.Splitter splitter1;

    }
}

