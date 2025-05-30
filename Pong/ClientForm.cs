using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Pong
{
    public class ClientForm : Form
    {
        Rectangle ball = new Rectangle(390, 200, 20, 20);
        Rectangle player1 = new Rectangle(30, 150, 20, 100);  // Host
        Rectangle player2 = new Rectangle(750, 150, 20, 100); // Client

        int score1 = 0, score2 = 0;
        int maxScore = 21;
        private TextBox ipTextBox;
        private Button connectButton;
        private Label infoLabel;
        private bool isConnected = false;

        TcpClient client;
        NetworkStream stream;
        System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();

        bool gameRunning = true;

        public ClientForm()
        {
            this.DoubleBuffered = true;
            this.Size = new Size(800, 450);
            this.Text = "Pong Client";
            this.BackColor = Color.Black;

            SetupConnectionUI();
        }


        private void SetupConnectionUI()
        {
            infoLabel = new Label()
            {
                Text = "IP-Adresse des Hosts eingeben:",
                ForeColor = Color.White,
                Location = new Point(250, 100),
                AutoSize = true
            };
            this.Controls.Add(infoLabel);

            ipTextBox = new TextBox()
            {
                Text = "192.168.1.100", // Beispiel-IP
                Location = new Point(250, 130),
                Width = 300
            };
            this.Controls.Add(ipTextBox);

            connectButton = new Button()
            {
                Text = "Verbinden",
                Location = new Point(250, 170),
                Width = 100
            };
            connectButton.Click += (s, e) => ConnectToServer(ipTextBox.Text);
            this.Controls.Add(connectButton);
        }

        private void ConnectToServer(string ip)
        {
            new Thread(() =>
            {
                try
                {
                    client = new TcpClient(ip, 5000);
                    stream = client.GetStream();

                    isConnected = true;
                    Invoke(() =>
                    {
                        this.Controls.Clear();
                        this.KeyDown += MovePlayer2;
                        this.Paint += DrawGame;

                        updateTimer.Interval = 16;
                        updateTimer.Tick += (s, e) => this.Invalidate();
                        updateTimer.Start();
                    });

                    new Thread(ReceiveGameState).Start();
                }
                catch (Exception ex)
                {
                    Invoke(() =>
                    {
                        MessageBox.Show("Verbindung fehlgeschlagen: " + ex.Message);
                    });
                }
            }).Start();
        }

        private void MovePlayer2(object sender, KeyEventArgs e)
        {
            if (!gameRunning) return;

            if (e.KeyCode == Keys.Up && player2.Y > 0)
                player2.Y -= 20;
            if (e.KeyCode == Keys.Down && player2.Y < this.ClientSize.Height - player2.Height)
                player2.Y += 20;

            SendPaddlePosition();
        }

        private void SendPaddlePosition()
        {
            if (stream == null) return;

            string msg = $"P2:{player2.Y}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            try { stream.Write(data, 0, data.Length); } catch { }
        }

        private void ReceiveGameState()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int len = stream.Read(buffer, 0, buffer.Length);
                    if (len > 0)
                    {
                        string received = Encoding.UTF8.GetString(buffer, 0, len);

                        // Nachrichten splitten, falls mehrere auf einmal kommen
                        string[] parts = received.Split(new[] { "BALL:" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string part in parts)
                        {
                            string msg = part.StartsWith("BALL:") ? part : "BALL:" + part.Trim();

                            // 👉 Prüfe auf GAMEOVER
                            if (msg.StartsWith("GAMEOVER:"))
                            {
                                string outcome = msg.Substring("GAMEOVER:".Length);
                                string text = outcome == "WIN" ? "Du hast gewonnen!" : "Du hast verloren.";
                                Invoke(new Action(() => ShowEndScreen(text)));
                                return; // Verbindung wird danach nicht weiter benötigt
                            }

                            // 🎯 Normale Spielnachricht verarbeiten
                            ParseGameState(msg);
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Verbindung zum Server verloren.");
            }
        }

        private void ParseGameState(string msg)
        {
            try
            {
                string[] pieces = msg.Split(';');
                foreach (string piece in pieces)
                {
                    if (piece.StartsWith("BALL:"))
                    {
                        string[] coords = piece.Substring(5).Split(',');
                        ball.X = int.Parse(coords[0]);
                        ball.Y = int.Parse(coords[1]);
                    }
                    else if (piece.StartsWith("P1:"))
                    {
                        player1.Y = int.Parse(piece.Substring(3));
                    }
                    else if (piece.StartsWith("P2:"))
                    {
                        player2.Y = int.Parse(piece.Substring(3));
                    }
                    else if (piece.StartsWith("SCORE:"))
                    {
                        string[] scores = piece.Substring(6).Split(':');
                        score1 = int.Parse(scores[0]);
                        score2 = int.Parse(scores[1]);
                        CheckGameOver();
                    }
                }
            }
            catch
            {
                // Ungültige Daten ignorieren
            }
        }
        private void CheckGameOver()
        {
            if (score1 >= maxScore || score2 >= maxScore)
            {
                gameRunning = false;
                updateTimer.Stop();

                string message = score2 > score1 ? "Du hast gewonnen!" : "Du hast verloren.";
                ShowGameOver(message);
            }
        }

        private void ShowGameOver(string message)
        {
            Invoke(() =>
            {
                Label resultLabel = new Label
                {
                    Text = message,
                    ForeColor = Color.White,
                    BackColor = Color.Black,
                    Font = new Font("Arial", 24),
                    AutoSize = true,
                    Location = new Point(Width / 2 - 100, Height / 2 - 60)
                };
                Controls.Add(resultLabel);

                Button restartBtn = new Button
                {
                    Text = "Neustarten",
                    Location = new Point(Width / 2 - 120, Height / 2),
                    Width = 100,
                    BackColor = Color.Black,
                    ForeColor = Color.White
                };
                restartBtn.Click += (s, e) => Application.Restart();
                Controls.Add(restartBtn);

                Button exitBtn = new Button
                {
                    Text = "Beenden",
                    Location = new Point(Width / 2 + 20, Height / 2),
                    Width = 100,
                    BackColor = Color.Black,
                    ForeColor = Color.White
                };
                exitBtn.Click += (s, e) => Application.Exit();
                Controls.Add(exitBtn);
            });
        }

        private void DrawGame(object sender, PaintEventArgs e)
        {
            if (!gameRunning) return;

            var g = e.Graphics;
            g.Clear(Color.Black);

            g.FillEllipse(Brushes.White, ball);
            g.FillRectangle(Brushes.White, player1);
            g.FillRectangle(Brushes.White, player2);
            g.DrawString($"{score1} : {score2}", new Font("Arial", 24), Brushes.White, Width / 2 - 50, 20);
        }
        private void ShowEndScreen(string result)
        {
            Label endLabel = new Label
            {
                Text = result,
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Arial", 24, FontStyle.Bold),
                Location = new Point(this.Width / 2 - 150, this.Height / 2 - 50)
            };

            Button restartBtn = new Button
            {
                Text = "Neustarten",
                Location = new Point(this.Width / 2 - 100, this.Height / 2 + 20),
                Width = 100
            };
            restartBtn.Click += (s, e) =>
            {
                Application.Restart(); // oder Methode zum Reset aufrufen
            };

            Button exitBtn = new Button
            {
                Text = "Beenden",
                Location = new Point(this.Width / 2, this.Height / 2 + 20),
                Width = 100
            };
            exitBtn.Click += (s, e) =>
            {
                this.Close();
            };

            this.Invoke(() =>
            {
                this.Controls.Add(endLabel);
                this.Controls.Add(restartBtn);
                this.Controls.Add(exitBtn);
            });
        }
    }

}

