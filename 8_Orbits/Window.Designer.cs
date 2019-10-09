using System.Drawing;

namespace Eight_Orbits {
	partial class Window {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.output = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// output
			// 
			this.output.AutoSize = true;
			this.output.Font = new System.Drawing.Font("Consolas", 9F);
			this.output.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.output.Location = new System.Drawing.Point(0, 0);
			this.output.Name = "output";
			this.output.Size = new System.Drawing.Size(0, 14);
			this.output.TabIndex = 0;
			// 
			// Window
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(512, 312);
			this.Controls.Add(this.output);
			this.Name = "Window";
			this.Text = "8 Orbits";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label output;
	}
}