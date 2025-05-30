using Pong;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pong
{
    public partial class StartForm : Form
    {
        public StartForm()
        {
            this.Text = "Pong - Auswahl";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterScreen;

            Button btnHost = new Button
            {
                Text = "Host starten",
                Location = new Point(50, 30),
                Size = new Size(180, 30)
            };
            btnHost.Click += (s, e) =>
            {
                Hide();
                new Form1().Show();
            };

            Button btnClient = new Button
            {
                Text = "Client starten",
                Location = new Point(50, 70),
                Size = new Size(180, 30)
            };
            btnClient.Click += (s, e) =>
            {
                Hide();
                new ClientForm().Show();
            };

            Controls.Add(btnHost);
            Controls.Add(btnClient);
        }
    }
}

