using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Pong
{
    public partial class Form1 : Form
    {
        Rectangle ball = new Rectangle(390, 200, 20, 20);
        Rectangle player1 = new Rectangle(30, 150, 20, 100);
        Rectangle player2 = new Rectangle(750, 150, 20, 100);

        int ballX = 5, ballY = 5;
        int score1 = 0, score2 = 0;
        int maxScore = 21;

        TcpListener server;
        TcpClient client;
        NetworkStream stream;

        System.Windows.Forms.Timer gameTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer sendTimer = new System.Windows.Forms.Timer();

        Label labelInfo, labelRounds;
        TrackBar slider;
        private Label ipLabel;
        Button btnStart;
        bool gameStarted = false;

        public Form1()
        {
            this.DoubleBuffered = true;
            this.Size = new Size(800, 450);
            this.Text = "Pong Server (Host)";
            this.KeyDown += MovePlayer1;
            this.Paint += DrawGame;


            SetupStartUI();
            StartServer();
        }

        private void SetupStartUI()
        {
            labelInfo = new Label() { Text = "Warte auf Client...", ForeColor = Color.White, BackColor = Color.Black, Location = new Point(300, 20), AutoSize = true };
            this.Controls.Add(labelInfo);

            slider = new TrackBar() { Minimum = 11, Maximum = 51, Value = 21, Location = new Point(300, 60), Width = 200 };
            slider.ValueChanged += (s, e) => UpdateRundenLabel();
            this.Controls.Add(slider);

            labelRounds = new Label() { Text = "Spielziel: 21 Punkte (21 Runden)", ForeColor = Color.White, BackColor = Color.Black, Location = new Point(300, 100), AutoSize = true };
            this.Controls.Add(labelRounds);

            btnStart = new Button() { Text = "Spiel starten", Location = new Point(300, 140), Enabled = false };
            btnStart.ForeColor = Color.White;  // Setzt die Textfarbe auf Weiß
            btnStart.Click += (s, e) => StartGame();
            this.Controls.Add(btnStart);
            ipLabel = new Label
            {
                Text = "Server-IP: wird ermittelt...",
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            this.Controls.Add(ipLabel);

            // Jetzt aktualisieren wir den Text mit der echten IP:
            ipLabel.Text = "Server-IP: " + GetLocalIPv4();
            this.Controls.Add(ipLabel);

            this.BackColor = Color.Black;
        }

        private void UpdateRundenLabel()
        {
            maxScore = slider.Value;
            labelRounds.Text = $"Spielziel: {maxScore} Punkte ({maxScore} Runden)";
        }

        private void StartServer()
        {
            new Thread(() =>
            {
                server = new TcpListener(IPAddress.Any, 5000);
                server.Start();
                client = server.AcceptTcpClient();
                stream = client.GetStream();

                Invoke(() =>
                {
                    labelInfo.Text = "Client verbunden!";
                    btnStart.Enabled = true;
                });

                new Thread(ReceiveClientPaddle).Start();
            }).Start();
        }

        private void StartGame()
        {
            this.Controls.Clear();
            gameStarted = true;

            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            sendTimer.Interval = 5;
            sendTimer.Tick += SendGameState;
            sendTimer.Start();
        }

        private void ReceiveClientPaddle()
        {
            byte[] buffer = new byte[256];
            try
            {
                while (true)
                {
                    int len = stream.Read(buffer, 0, buffer.Length);
                    if (len > 0)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, len);
                        if (msg.StartsWith("P2:"))
                        {
                            if (int.TryParse(msg.Substring(3), out int y))
                            {
                                player2.Y = y;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void SendGameState(object sender, EventArgs e)
        {
            if (stream == null) return;
            string data = $"BALL:{ball.X},{ball.Y};P1:{player1.Y};P2:{player2.Y};SCORE:{score1}:{score2}";
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            try { stream.Write(bytes, 0, bytes.Length); } catch { }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            ball.X += ballX;
            ball.Y += ballY;

            if (ball.Y <= 0 || ball.Y >= ClientSize.Height - ball.Height)
                ballY = -ballY;

            if (ball.IntersectsWith(player1)) ballX = Math.Abs(ballX);
            if (ball.IntersectsWith(player2)) ballX = -Math.Abs(ballX);

            if (ball.X < 0) { score2++; CheckScore(); ResetBall(); }
            if (ball.X > ClientSize.Width) { score1++; CheckScore(); ResetBall(); }

            Invalidate();
        }

        private void CheckScore()
        {
            if (score1 >= maxScore || score2 >= maxScore)
            {
                gameTimer.Stop();
                sendTimer.Stop();

                string result = score1 > score2 ? "Du hast gewonnen!" : "Du hast verloren.";
             
                // Game Over Nachricht an Client senden
                string msg = score1 > score2 ? "GAMEOVER:WIN" : "GAMEOVER:LOSE";
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(msg);
                    stream.Write(data, 0, data.Length);
                }
                catch { }

                ShowEndScreen(result);
            }
        }


        private void ResetBall()
        {
            ball.X = ClientSize.Width / 2 - ball.Width / 2;
            ball.Y = ClientSize.Height / 2 - ball.Height / 2;
            ballX = -ballX;
        }

        private void MovePlayer1(object sender, KeyEventArgs e)
        {
            if (!gameStarted) return;
            if (e.KeyCode == Keys.Up && player1.Y > 0) player1.Y -= 20;
            if (e.KeyCode == Keys.Down && player1.Y < ClientSize.Height - player1.Height) player1.Y += 20;
        }

        private void DrawGame(object sender, PaintEventArgs e)
        {
            if (!gameStarted) return;
            var g = e.Graphics;
            g.Clear(Color.Black);
            g.FillEllipse(Brushes.White, ball);
            g.FillRectangle(Brushes.White, player1);
            g.FillRectangle(Brushes.White, player2);
            g.DrawString($"{score1} : {score2}", new Font("Arial", 24), Brushes.White, Width / 2 - 50, 20);
        }


        private void SendGameOver(string result)
        {
            if (stream == null) return;

            string msg = $"GAMEOVER:{result}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            try
            {
                stream.Write(data, 0, data.Length); // Nachricht senden
            }
            catch { }
        }

        private void ShowGameOver(bool won)
        {
            Label msg = new Label
            {
                Text = won ? "Du hast gewonnen!" : "Du hast verloren.",
                ForeColor = Color.White,
                BackColor = Color.Black,
                Font = new Font("Arial", 24),
                AutoSize = true,
                Location = new Point(Width / 2 - 150, Height / 2 - 100)
            };
            Controls.Add(msg);

            Button restartBtn = new Button
            {
                Text = "Neustarten",
                Location = new Point(Width / 2 - 100, Height / 2),
                Width = 100,
                ForeColor = Color.White
            };
            restartBtn.Click += (s, e) => RestartGame();
            Controls.Add(restartBtn);

            Button exitBtn = new Button
            {
                Text = "Beenden",
                Location = new Point(Width / 2 + 10, Height / 2),
                Width = 100,
                ForeColor = Color.White
            };
            exitBtn.Click += (s, e) => Application.Exit();
            Controls.Add(exitBtn);
        }

        // Neustart des Spiels
        private void RestartGame()
        {
            // Hier kann der Server zurückgesetzt werden, wenn nötig.
            score1 = 0;
            score2 = 0;
            ball.X = ClientSize.Width / 2 - ball.Width / 2;
            ball.Y = ClientSize.Height / 2 - ball.Height / 2;
            gameStarted = false;
            Controls.Clear();
            SetupStartUI();
        }
        private void ShowEndScreen(string message)
        {
            this.Controls.Clear();
            this.BackColor = Color.Black;

            Label label = new Label
            {
                Text = message,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Font = new Font("Arial", 24),
                AutoSize = true,
                Location = new Point(Width / 2 - 150, Height / 2 - 100)
            };
            this.Controls.Add(label);

            Button restartBtn = new Button
            {
                Text = "Neustarten",
                ForeColor = Color.White,
                BackColor = Color.Black,
                Location = new Point(Width / 2 - 110, Height / 2),
                Width = 100
            };
            restartBtn.Click += (s, e) => Application.Restart();
            this.Controls.Add(restartBtn);

            Button exitBtn = new Button
            {
                Text = "Beenden",
                ForeColor = Color.White,
                BackColor = Color.Black,
                Location = new Point(Width / 2 + 10, Height / 2),
                Width = 100
            };
            exitBtn.Click += (s, e) => Application.Exit();
            this.Controls.Add(exitBtn);
        }
        public static string GetLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    var ipProps = ni.GetIPProperties();
                    foreach (var ua in ipProps.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ua.Address))
                        {
                            return ua.Address.ToString();
                        }
                    }
                }
            }
            return "IP nicht gefunden";
        }
    }
}