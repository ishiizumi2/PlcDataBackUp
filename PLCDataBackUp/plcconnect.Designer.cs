namespace PLCDataBackUp
{
    partial class plcconnect
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.txt_Host = new System.Windows.Forms.TextBox();
            this.txt_Port = new System.Windows.Forms.TextBox();
            this.btn_Connect = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.StartAdd1 = new System.Windows.Forms.TextBox();
            this.EndAdd1 = new System.Windows.Forms.TextBox();
            this.EndAdd2 = new System.Windows.Forms.TextBox();
            this.StartAdd2 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.EndAdd3 = new System.Windows.Forms.TextBox();
            this.StartAdd3 = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.LoadBtn = new System.Windows.Forms.Button();
            this.WriteBtn = new System.Windows.Forms.Button();
            this.ReadBtn = new System.Windows.Forms.Button();
            this.textBox10 = new System.Windows.Forms.TextBox();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.SaveBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.button3 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.button4 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // txt_Host
            // 
            this.txt_Host.Location = new System.Drawing.Point(37, 44);
            this.txt_Host.Name = "txt_Host";
            this.txt_Host.Size = new System.Drawing.Size(100, 19);
            this.txt_Host.TabIndex = 0;
            this.txt_Host.Text = "192.0.1.10";
            // 
            // txt_Port
            // 
            this.txt_Port.Location = new System.Drawing.Point(37, 93);
            this.txt_Port.Name = "txt_Port";
            this.txt_Port.Size = new System.Drawing.Size(100, 19);
            this.txt_Port.TabIndex = 1;
            this.txt_Port.Text = "1280";
            // 
            // btn_Connect
            // 
            this.btn_Connect.Location = new System.Drawing.Point(26, 139);
            this.btn_Connect.Name = "btn_Connect";
            this.btn_Connect.Size = new System.Drawing.Size(134, 23);
            this.btn_Connect.TabIndex = 4;
            this.btn_Connect.Text = "接続";
            this.btn_Connect.UseVisualStyleBackColor = true;
            this.btn_Connect.Click += new System.EventHandler(this.btn_Connect_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(38, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 12);
            this.label3.TabIndex = 9;
            this.label3.Text = "PLC IP Addres";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(38, 78);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 12);
            this.label4.TabIndex = 10;
            this.label4.Text = "PLC PortNo";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(190, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(54, 12);
            this.label5.TabIndex = 11;
            this.label5.Text = "Ｄ Addres";
            // 
            // StartAdd1
            // 
            this.StartAdd1.Location = new System.Drawing.Point(178, 44);
            this.StartAdd1.Name = "StartAdd1";
            this.StartAdd1.Size = new System.Drawing.Size(100, 19);
            this.StartAdd1.TabIndex = 12;
            this.StartAdd1.Text = "0";
            // 
            // EndAdd1
            // 
            this.EndAdd1.Location = new System.Drawing.Point(178, 93);
            this.EndAdd1.Name = "EndAdd1";
            this.EndAdd1.Size = new System.Drawing.Size(100, 19);
            this.EndAdd1.TabIndex = 13;
            this.EndAdd1.Text = "10000";
            // 
            // EndAdd2
            // 
            this.EndAdd2.Location = new System.Drawing.Point(293, 93);
            this.EndAdd2.Name = "EndAdd2";
            this.EndAdd2.Size = new System.Drawing.Size(100, 19);
            this.EndAdd2.TabIndex = 16;
            this.EndAdd2.Text = "0";
            // 
            // StartAdd2
            // 
            this.StartAdd2.Location = new System.Drawing.Point(293, 44);
            this.StartAdd2.Name = "StartAdd2";
            this.StartAdd2.Size = new System.Drawing.Size(100, 19);
            this.StartAdd2.TabIndex = 15;
            this.StartAdd2.Text = "0";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(305, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "R Addres";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(190, 29);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(30, 12);
            this.label7.TabIndex = 17;
            this.label7.Text = "Start";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(190, 78);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(24, 12);
            this.label8.TabIndex = 18;
            this.label8.Text = "End";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(305, 78);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(24, 12);
            this.label9.TabIndex = 20;
            this.label9.Text = "End";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(305, 29);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(30, 12);
            this.label10.TabIndex = 19;
            this.label10.Text = "Start";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(424, 78);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(24, 12);
            this.label11.TabIndex = 25;
            this.label11.Text = "End";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(424, 29);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(30, 12);
            this.label12.TabIndex = 24;
            this.label12.Text = "Start";
            // 
            // EndAdd3
            // 
            this.EndAdd3.Location = new System.Drawing.Point(412, 93);
            this.EndAdd3.Name = "EndAdd3";
            this.EndAdd3.Size = new System.Drawing.Size(100, 19);
            this.EndAdd3.TabIndex = 23;
            this.EndAdd3.Text = "0";
            // 
            // StartAdd3
            // 
            this.StartAdd3.Location = new System.Drawing.Point(412, 44);
            this.StartAdd3.Name = "StartAdd3";
            this.StartAdd3.Size = new System.Drawing.Size(100, 19);
            this.StartAdd3.TabIndex = 22;
            this.StartAdd3.Text = "0";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(424, 9);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(54, 12);
            this.label13.TabIndex = 21;
            this.label13.Text = "W Addres";
            // 
            // LoadBtn
            // 
            this.LoadBtn.Location = new System.Drawing.Point(26, 184);
            this.LoadBtn.Name = "LoadBtn";
            this.LoadBtn.Size = new System.Drawing.Size(134, 23);
            this.LoadBtn.TabIndex = 26;
            this.LoadBtn.Text = "連続データの読込";
            this.LoadBtn.UseVisualStyleBackColor = true;
            this.LoadBtn.Click += new System.EventHandler(this.button1_Click);
            // 
            // WriteBtn
            // 
            this.WriteBtn.Location = new System.Drawing.Point(26, 223);
            this.WriteBtn.Name = "WriteBtn";
            this.WriteBtn.Size = new System.Drawing.Size(134, 23);
            this.WriteBtn.TabIndex = 27;
            this.WriteBtn.Text = "連続データの書込";
            this.WriteBtn.UseVisualStyleBackColor = true;
            this.WriteBtn.Click += new System.EventHandler(this.Write_Click);
            // 
            // ReadBtn
            // 
            this.ReadBtn.Location = new System.Drawing.Point(26, 266);
            this.ReadBtn.Name = "ReadBtn";
            this.ReadBtn.Size = new System.Drawing.Size(134, 23);
            this.ReadBtn.TabIndex = 28;
            this.ReadBtn.Text = "連続データファイルの読出";
            this.ReadBtn.UseVisualStyleBackColor = true;
            this.ReadBtn.Click += new System.EventHandler(this.button3_Click);
            // 
            // textBox10
            // 
            this.textBox10.Location = new System.Drawing.Point(630, 44);
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new System.Drawing.Size(100, 19);
            this.textBox10.TabIndex = 34;
            this.textBox10.Text = "No Com";
            // 
            // dataGridView2
            // 
            this.dataGridView2.AllowUserToAddRows = false;
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(178, 139);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.RowTemplate.Height = 21;
            this.dataGridView2.Size = new System.Drawing.Size(270, 329);
            this.dataGridView2.TabIndex = 38;
            // 
            // SaveBtn
            // 
            this.SaveBtn.Location = new System.Drawing.Point(26, 309);
            this.SaveBtn.Name = "SaveBtn";
            this.SaveBtn.Size = new System.Drawing.Size(134, 23);
            this.SaveBtn.TabIndex = 39;
            this.SaveBtn.Text = "連続データファイルの保存";
            this.SaveBtn.UseVisualStyleBackColor = true;
            this.SaveBtn.Click += new System.EventHandler(this.button6_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(638, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 40;
            this.label1.Text = "通信状態";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(26, 360);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(134, 23);
            this.button2.TabIndex = 43;
            this.button2.Text = "ランダム読み出し";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(471, 139);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.Size = new System.Drawing.Size(353, 329);
            this.dataGridView1.TabIndex = 36;
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(26, 445);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(134, 23);
            this.button3.TabIndex = 45;
            this.button3.Text = "ランダム書き込み";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click_1);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(178, 498);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(646, 19);
            this.textBox1.TabIndex = 46;
            // 
            // timer2
            // 
            this.timer2.Interval = 1000;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(26, 403);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(134, 23);
            this.button4.TabIndex = 47;
            this.button4.Text = "タイマーStop";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(630, 93);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(100, 19);
            this.textBox2.TabIndex = 48;
            this.textBox2.Text = "1";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(638, 78);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(67, 12);
            this.label14.TabIndex = 49;
            this.label14.Text = "タイマー(sec)";
            // 
            // plcconnect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(836, 607);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SaveBtn);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.textBox10);
            this.Controls.Add(this.ReadBtn);
            this.Controls.Add(this.WriteBtn);
            this.Controls.Add(this.LoadBtn);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.EndAdd3);
            this.Controls.Add(this.StartAdd3);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.EndAdd2);
            this.Controls.Add(this.StartAdd2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.EndAdd1);
            this.Controls.Add(this.StartAdd1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btn_Connect);
            this.Controls.Add(this.txt_Port);
            this.Controls.Add(this.txt_Host);
            this.Name = "plcconnect";
            this.Text = " PlcDataBackup";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txt_Host;
        private System.Windows.Forms.TextBox txt_Port;
        private System.Windows.Forms.Button btn_Connect;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox StartAdd1;
        private System.Windows.Forms.TextBox EndAdd1;
        private System.Windows.Forms.TextBox EndAdd2;
        private System.Windows.Forms.TextBox StartAdd2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox EndAdd3;
        private System.Windows.Forms.TextBox StartAdd3;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button LoadBtn;
        private System.Windows.Forms.Button WriteBtn;
        private System.Windows.Forms.Button ReadBtn;
        private System.Windows.Forms.TextBox textBox10;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.Button SaveBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label14;
    }
}

