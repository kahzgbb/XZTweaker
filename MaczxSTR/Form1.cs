using System;
using System.Drawing;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MaczxSTR
{
    public partial class Form1 : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect,
        int nTopRect,
        int nRightRect,
        int nBottomRect,
        int nWidthEllipse,
        int nHeightEllipse
        );

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hProcess);


        [DllImport("Gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, int dwFlags);

        private const int SHERB_NOCONFIRMATION = 0x00000001;
        private const int SHERB_NOPROGRESSUI = 0x00000002;
        private const int SHERB_NOSOUND = 0x00000004;

        private System.Windows.Forms.Timer animationTimer4;
        private System.Windows.Forms.Timer animationTimer5;
        private Color startColor4;
        private Color targetColor4;
        private Color startColor5;
        private Color targetColor5;
        private float animationProgress4;
        private float animationProgress5;
        private const float animationStep = 1f / (0.2f * 60);
        private Ping pingSender;
        private bool isPinging;
        private Thread pingThread;
        private bool isDragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public Form1()
        {
            InitializeComponent();

            this.MouseDown += Form1_MouseDown;
            this.MouseMove += Form1_MouseMove;
            this.MouseUp += Form1_MouseUp;

            button1.BackColor = Color.Transparent;
            button1.MouseEnter += Button1_MouseEnter;
            button1.MouseLeave += Button1_MouseLeave;

            pingSender = new Ping();

            animationTimer4 = new System.Windows.Forms.Timer { Interval = 16 };
            animationTimer5 = new System.Windows.Forms.Timer { Interval = 16 };

            animationTimer4.Tick += (s, e) => AnimateLabel(label4, ref animationTimer4, ref startColor4, ref targetColor4, ref animationProgress4);
            animationTimer5.Tick += (s, e) => AnimateLabel(label5, ref animationTimer5, ref startColor5, ref targetColor5, ref animationProgress5);

            label4.MouseEnter += (s, e) => StartAnimation(label4, ref animationTimer4, ref startColor4, ref targetColor4, ref animationProgress4, Color.Red);
            label4.MouseLeave += (s, e) => StartAnimation(label4, ref animationTimer4, ref startColor4, ref targetColor4, ref animationProgress4, SystemColors.ControlLightLight);

            label5.MouseEnter += (s, e) => StartAnimation(label5, ref animationTimer5, ref startColor5, ref targetColor5, ref animationProgress5, Color.Red);
            label5.MouseLeave += (s, e) => StartAnimation(label5, ref animationTimer5, ref startColor5, ref targetColor5, ref animationProgress5, SystemColors.ControlLightLight);
            machinesettings();
        }

        private void StartAnimation(Label label, ref System.Windows.Forms.Timer timer, ref Color startColor, ref Color targetColor, ref float progress, Color newTargetColor)
        {
            if (timer.Enabled)
            {
                startColor = label.ForeColor;
            }
            else
            {
                startColor = label.ForeColor;
            }

            targetColor = newTargetColor;
            progress = 0;
            timer.Start();
        }

        private void AnimateLabel(Label label, ref System.Windows.Forms.Timer timer, ref Color startColor, ref Color targetColor, ref float progress)
        {
            progress += animationStep;

            if (progress >= 1)
            {
                progress = 1;
                timer.Stop();
            }

            int r = (int)(startColor.R + (targetColor.R - startColor.R) * progress);
            int g = (int)(startColor.G + (targetColor.G - startColor.G) * progress);
            int b = (int)(startColor.B + (targetColor.B - startColor.B) * progress);

            label.ForeColor = Color.FromArgb(r, g, b);
        }

        private void label4_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void label5_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void kryptonButton1_Click(object sender, EventArgs e)
        {

            try
            {
                Cursor = Cursors.WaitCursor;

                // Encontra o adaptador de rede ativo com conexão à internet
                NetworkInterface activeAdapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(nic =>
                        nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        nic.GetIPProperties().GatewayAddresses.Count > 0);

                if (activeAdapter != null)
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true"))
                    {
                        foreach (ManagementObject adapter in searcher.Get())
                        {
                            string description = adapter["Description"].ToString();
                            if (description == activeAdapter.Description)
                            {
                                ManagementBaseObject newDNS = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                                newDNS["DNSServerSearchOrder"] = new string[] { "1.1.1.1", "1.0.0.1" };
                                adapter.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                                adapter.InvokeMethod("ReleaseDHCPLease", null);
                                adapter.InvokeMethod("RenewDHCPLease", null);

                                MessageBox.Show($"DNS Changed to Cloudflare\n" +
                                              $"Adaptador: {activeAdapter.Name}\n" +
                                              "DNS 1: 1.1.1.1\n" +
                                              "DNS 2: 1.0.0.1",
                                              "Sucesso",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Information);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Check your wifi connection",
                                  "Erro",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dns change error: {ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void kryptonButton2_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                NetworkInterface activeAdapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(nic =>
                        nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        nic.GetIPProperties().GatewayAddresses.Count > 0);

                if (activeAdapter != null)
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true"))
                    {
                        foreach (ManagementObject adapter in searcher.Get())
                        {
                            string description = adapter["Description"].ToString();
                            if (description == activeAdapter.Description)
                            {
                                ManagementBaseObject newDNS = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                                newDNS["DNSServerSearchOrder"] = new string[] { "208.67.222.222", "208.67.222.220" };
                                adapter.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                                adapter.InvokeMethod("ReleaseDHCPLease", null);
                                adapter.InvokeMethod("RenewDHCPLease", null);
                                MessageBox.Show($"DNS Changed to OpenDNS\n" +
                                              $"Adaptador: {activeAdapter.Name}\n" +
                                              "DNS Primário: 208.67.222.222\n" +
                                              "DNS Secundário: 208.67.222.220",
                                              "Sucesso",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Information);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Check your wifi connection",
                                  "Error",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dns Change error: {ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GetDns();
            if (isPinging)
            {
                pingstop();
            }
            else
            {
                ping();
            }
        }

        private void ping()
        {
            isPinging = true;
            button1.Text = "Stop Test";

            pingThread = new Thread(() =>
            {
                while (isPinging)
                {
                    string host = string.IsNullOrWhiteSpace(textBox1.Text) ? "google.com" : textBox1.Text.Trim();
                    try
                    {
                        var reply = pingSender.Send(host);
                        this.Invoke(new Action(() => label6.Text = reply.Status == IPStatus.Success ? $"Ping: {reply.RoundtripTime}ms" : $"Ping error: {reply.Status}"));
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() => label6.Text = $"Ping Error: {ex.Message}"));
                    }
                    Thread.Sleep(1000);
                }
            });

            pingThread.IsBackground = true;
            pingThread.Start();
        }

        private void pingstop()
        {
            isPinging = false;
            button1.Text = "Start Test";
            pingThread?.Join();
        }

        private void Button1_MouseEnter(object sender, EventArgs e)
        {
            button1.BackColor = Color.Red;
        }

        private void Button1_MouseLeave(object sender, EventArgs e)
        {
            button1.BackColor = Color.Transparent;
        }

        private void GetDns()
        {
            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface networkInterface in networkInterfaces)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                        if (ipProperties.DnsAddresses.Count > 0)
                        {
                            label3.Text = ipProperties.DnsAddresses[0].ToString();
                            if (ipProperties.DnsAddresses.Count > 1)
                            {
                                label7.Text = ipProperties.DnsAddresses[1].ToString();
                            }
                            else
                            {
                                label7.Text = "N/A";
                            }
                            return;
                        }
                    }
                }
                label3.Text = "Unknown DNS";
                label7.Text = "Unknown DNS";
            }
            catch (Exception ex)
            {
                label3.Text = $"Error: {ex.Message}";
                label7.Text = "Error getting DNS";
            }
        }

        private void kryptonPictureBox3_Click(object sender, EventArgs e)
        {
            panel2.Visible = true;
            panel6.Visible = false;
            panel7.Visible = false;
            panel12.Visible = false;
            panel13.Visible = false;
        }

        private void kryptonPictureBox1_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            panel6.Visible = false;
            panel7.Visible = true;
            panel12.Visible = false;
            panel13.Visible = false;
        }

        private void kryptonPictureBox4_Click(object sender, EventArgs e)
        {
            panel6.Visible = true;
            panel2.Visible = false;
            panel7.Visible = false;
            panel12.Visible = false;
            panel13.Visible = false;
        }

        private void kryptonPictureBox5_Click(object sender, EventArgs e)
        {
            panel12.Visible = true;
            panel6.Visible = false;
            panel2.Visible = false;
            panel7.Visible = false;
            panel13.Visible = false;
        }

        private void kryptonPictureBox6_Click(object sender, EventArgs e)
        {
            panel12.Visible = false;
            panel6.Visible = false;
            panel2.Visible = false;
            panel7.Visible = false;
            panel13.Visible = true;
        }

        private void kryptonPictureBox2_Click(object sender, EventArgs e)
        {
            panel12.Visible = false;
            panel6.Visible = false;
            panel2.Visible = false;
            panel7.Visible = false;
            panel13.Visible = false;
        }

        private void kryptonButton3_Click(object sender, EventArgs e)
        {
            DialogResult continueResult = MessageBox.Show("Do you wish to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (continueResult == DialogResult.No)
            {
                return;
            }

            DialogResult restorePointResult = MessageBox.Show("Create Restore Point?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (restorePointResult == DialogResult.Yes)
            {
                string restorePointName = GenerateRandomName(6);
                CreateRestorePoint(restorePointName);
            }

            string backupFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old_Regedit.txt");
            BackupRegistryValues(backupFilePath);
            MessageBox.Show("Regedit Backup Created in Directory.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ApplyRegistrySettings();
            MessageBox.Show("Regedit Changed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GenerateRandomName(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[new Random().Next(s.Length)]).ToArray());
        }

        private void CreateRestorePoint(string name)
        {
            try
            {
                ManagementScope scope = new ManagementScope("\\\\.\\root\\default");
                ManagementClass mc = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);
                ManagementBaseObject inParams = mc.GetMethodParameters("CreateRestorePoint");
                inParams["Description"] = name;
                inParams["RestorePointType"] = 12; // MODIFY_SETTINGS
                inParams["EventType"] = 100;
                mc.InvokeMethod("CreateRestorePoint", inParams, null);
                MessageBox.Show("Restore point created.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BackupRegistryValues(string filePath)
        {
            try
            {
                StringBuilder backupData = new StringBuilder();

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            object value = key.GetValue(valueName);
                            string valueType = key.GetValueKind(valueName).ToString();
                            backupData.AppendLine($"{valueName}={value} ({valueType})");
                        }
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows"))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            object value = key.GetValue(valueName);
                            string valueType = key.GetValueKind(valueName).ToString();
                            backupData.AppendLine($"{valueName}={value} ({valueType})");
                        }
                    }
                }

                File.WriteAllText(filePath, backupData.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Regedit backup error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Reac()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip"))
                {
                    key.SetValue("MTU", 1280, RegistryValueKind.DWord);
                    key.SetValue("MSS", 1280, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows"))
                {
                    key.SetValue("NonBestEffortLimit", 0, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Registration error: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void backupreac(string filePath)
        {
            try
            {
                StringBuilder backupData = new StringBuilder();

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip"))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            object value = key.GetValue(valueName);
                            string valueType = key.GetValueKind(valueName).ToString();
                            backupData.AppendLine($"{valueName}={value} ({valueType})");
                        }
                    }
                }

                File.WriteAllText(filePath, backupData.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup Regedit Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyRegistrySettings()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
                {
                    key.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                    key.SetValue("TcpMaxDataRetransmissions", 5, RegistryValueKind.DWord);
                    key.SetValue("TCPDelAckTicks", 0, RegistryValueKind.DWord);
                    key.SetValue("Tcp1323Opts", 1, RegistryValueKind.DWord);
                    key.SetValue("MaxFreeTcbs", 65536, RegistryValueKind.DWord);
                    key.SetValue("MaxUserPort", 65534, RegistryValueKind.DWord);
                    key.SetValue("DefaultTTL", 100, RegistryValueKind.DWord);
                    key.SetValue("GlobalMaxTcpWindowSize", 65535, RegistryValueKind.DWord);
                    key.SetValue("MaxConnectionsPerServer", 22, RegistryValueKind.DWord);
                    key.SetValue("IRPStackSize", 50, RegistryValueKind.DWord);
                    key.SetValue("MTU", 1500, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows"))
                {
                    key.SetValue("NonBestEffortLimit", 0, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Registration error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton4_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you wish to continue?",
                                                  "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                return;
            }

            try
            {
                int bestPacketSize = FindBestPacketSize("8.8.8.8");
                if (bestPacketSize > 0)
                {
                    MessageBox.Show($"Found!\nYou best package size is: {bestPacketSize}", "Result",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Unable to determine best packet size.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int FindBestPacketSize(string host)
        {
            int startSize = 1500;
            int minSize = 1;
            int step = 10;
            int bestSize = 0;

            while (startSize >= minSize)
            {
                if (PingWithPacketSize(host, startSize))
                {
                    bestSize = startSize;
                    break;
                }
                startSize -= step;
            }

            return bestSize;
        }

        private bool PingWithPacketSize(string host, int packetSize)
        {
            try
            {
                byte[] buffer = new byte[packetSize];
                new Random().NextBytes(buffer);

                Ping ping = new Ping();
                PingOptions options = new PingOptions
                {
                    DontFragment = true
                };

                PingReply reply = ping.Send(host, 1000, buffer, options);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                isDragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = this.Location;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                isDragging = false;
            }
        }

        private void SetRoundedBorders(int borderRadius)
        {
            IntPtr regionHandle = CreateRoundRectRgn(0, 0, this.Width, this.Height, borderRadius, borderRadius);
            this.Region = Region.FromHrgn(regionHandle);
            DeleteObject(regionHandle);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            SetRoundedBorders(20);
        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void kryptonButton8_Click(object sender, EventArgs e)
        {
            DialogResult continueResult = MessageBox.Show("Do you wish to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (continueResult == DialogResult.No)
            {
                return;
            }

            DialogResult restorePointResult = MessageBox.Show("Create Restore Point?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (restorePointResult == DialogResult.Yes)
            {
                string restorePointName = GenerateRandomName(6);
                CreateRestorePoint(restorePointName);
            }

            string backupFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old_RegeditReac.txt");
            backupreac(backupFilePath);
            MessageBox.Show("Regedit backup created", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Reac();
            MessageBox.Show("Applied settings", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void kryptonButton9_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                // Encontra o adaptador de rede ativo com conexão à internet
                NetworkInterface activeAdapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(nic =>
                        nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        nic.GetIPProperties().GatewayAddresses.Count > 0);

                if (activeAdapter != null)
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true"))
                    {
                        foreach (ManagementObject adapter in searcher.Get())
                        {
                            string description = adapter["Description"].ToString();
                            if (description == activeAdapter.Description)
                            {
                                // Configura os novos DNS
                                ManagementBaseObject newDNS = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                                newDNS["DNSServerSearchOrder"] = new string[] { "8.8.8.8", "8.8.4.4" };
                                adapter.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);

                                // Libera e renova DHCP
                                adapter.InvokeMethod("ReleaseDHCPLease", null);
                                adapter.InvokeMethod("RenewDHCPLease", null);

                                // Limpa o cache DNS
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "ipconfig",
                                    Arguments = "/flushdns",
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                }).WaitForExit();

                                MessageBox.Show($"DNS Changed to OpenDNS\n" +
                                              $"Adaptador: {activeAdapter.Name}\n" +
                                              "DNS Primário: 8.8.8.8\n" +
                                              "DNS Secundário: 8.8.4.4",
                                              "Sucesso",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Information);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Nenhuma conexão de rede ativa encontrada.",
                                  "Erro",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar configurações de DNS: {ex.Message}",
                              "Erro",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }

        }

        private void kryptonButton6_Click(object sender, EventArgs e)
        {
            // big latency
            DialogResult continueResult = MessageBox.Show("Deseja Continuar?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (continueResult == DialogResult.No)
            {
                return;
            }

            DialogResult restorePointResult = MessageBox.Show("Criar Ponto de Restauração?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (restorePointResult == DialogResult.Yes)
            {
                string restorePointName = GenerateRandomName(6);
                CreateRestorePoint(restorePointName);
            }

            string backupFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old_BigLatReg.txt");
            backupreac(backupFilePath);
            MessageBox.Show("Backup de regedit criado", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            biglatencyreg();
            MessageBox.Show("Configurações aplicadas com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void kryptonButton10_Click(object sender, EventArgs e)
        {
            string tempPath = Path.GetTempPath();
            int deletedFilesCount = 0;

            try
            {
                string[] tempFiles = Directory.GetFiles(tempPath);

                foreach (string file in tempFiles)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch
                    {
                    }
                }

                string[] tempDirs = Directory.GetDirectories(tempPath);
                foreach (string dir in tempDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch
                    {
                    }
                }
                MessageBox.Show(
                    $"{deletedFilesCount} Deleted Files",
                    "Operation Completed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n{ex.Message}",
                    "Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

        }

        private void kryptonButton5_Click(object sender, EventArgs e)
        {
            string prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(prefetchPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(prefetchPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://discord.gg/qV9regtfQr",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton12_Click(object sender, EventArgs e)
        {
            try
            {
                var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", true);
                if (regKey != null)
                {
                    foreach (var valueName in regKey.GetValueNames())
                    {
                        regKey.DeleteValue(valueName);
                    }
                    regKey.Close();
                }
                MessageBox.Show("Run history cleared.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton14_Click(object sender, EventArgs e)
        {
            string folderPath = @"C:\Windows\SoftwareDistribution\Download";
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(folderPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton13_Click(object sender, EventArgs e)
        {
            string recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(recentPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(recentPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton15_Click(object sender, EventArgs e)
        {
            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                MessageBox.Show("Recycle Bin completely cleared.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton16_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\Microsoft\Windows\Recent\AutomaticDestinations");
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(path);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton17_Click(object sender, EventArgs e)
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-NoExit -Command Clear-Host",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                System.Diagnostics.Process.Start(processInfo);
                MessageBox.Show("PowerShell console cleared.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton18_Click(object sender, EventArgs e)
        {
            string folderPath = @"C:\Windows\appcompat\Programs\Install";
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(folderPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton19_Click(object sender, EventArgs e)
        {
            string folderPath = @"C:\Windows\Logs\WindowsUpdate";
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(folderPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton20_Click(object sender, EventArgs e)
        {
            string folderPath = @"C:\Windows\SystemTemp";
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(folderPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton21_Click(object sender, EventArgs e)
        {
            try
            {
                string crashDumpsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps");

                if (Directory.Exists(crashDumpsPath))
                {
                    var files = Directory.GetFiles(crashDumpsPath);

                    int deletedCount = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch
                        {
                        }
                    }

                    MessageBox.Show($"{deletedCount} files deleted from CrashDumps folder.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("CrashDumps folder does not exist.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton22_Click(object sender, EventArgs e)
        {
            try
            {
                string[] werPaths = {
            @"C:\ProgramData\Microsoft\Windows\WER\ReportArchive",
            @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue"
             };

                int totalDeletedFiles = 0;

                foreach (var path in werPaths)
                {
                    if (Directory.Exists(path))
                    {
                        var files = Directory.GetFiles(path);

                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                                totalDeletedFiles++;
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                MessageBox.Show($"{totalDeletedFiles} files deleted", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void machinesettings()
        {
            string cpu = string.Empty, gpu = string.Empty, ram = string.Empty, os = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    cpu = obj["Name"]?.ToString();
                    break;
                }

                searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    gpu = obj["Name"]?.ToString();
                    break;
                }

                searcher = new ManagementObjectSearcher("select * from Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    double totalRam = Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                    ram = $"{Math.Round(totalRam, 2)} GB";
                    break;
                }

                searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    os = obj["Caption"]?.ToString();
                    break;
                }
            }
            catch
            {
                cpu = gpu = ram = os = "Error retrieving information";
            }

            label22.Text = cpu;
            label23.Text = gpu;
            label24.Text = ram;
            label25.Text = os;
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://github.com/kahzgbb",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton23_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        EmptyWorkingSet(process.Handle);
                    }
                    catch
                    {
                    }
                }
                MessageBox.Show("Memory cleared", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton19_Click_1(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/setactive scheme_max",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                MessageBox.Show("Power plan set to High Performance.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void kryptonButton7_Click(object sender, EventArgs e)
        {
            DialogResult continueResult = MessageBox.Show("Do you wish to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (continueResult == DialogResult.No)
            {
                return;
            }

            DialogResult restorePointResult = MessageBox.Show("Create Restore Point?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (restorePointResult == DialogResult.Yes)
            {
                string restorePointName = GenerateRandomName(6);
                CreateRestorePoint(restorePointName);
            }

            string backupFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old_RegeditSumoReg.txt");
            backupreac(backupFilePath);
            MessageBox.Show("Regedit backup created", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SumoReg();
            MessageBox.Show("Applied settings", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SumoReg()
        {

            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
                {
                    key.SetValue("DefaultTTL", 0x40, RegistryValueKind.DWord);
                    key.SetValue("DisableTaskOffload", 0x01, RegistryValueKind.DWord);
                    key.SetValue("EnableConnectionRateLimiting", 0x00, RegistryValueKind.DWord);
                    key.SetValue("EnableDCA", 0x01, RegistryValueKind.DWord);
                    key.SetValue("EnablePMTUBHDetect", 0x00, RegistryValueKind.DWord);
                    key.SetValue("EnablePMTUDiscovery", 0x01, RegistryValueKind.DWord);
                    key.SetValue("EnableRSS", 0x01, RegistryValueKind.DWord);
                    key.SetValue("TcpTimedWaitDelay", 0x1E, RegistryValueKind.DWord);
                    key.SetValue("EnableWsd", 0x00, RegistryValueKind.DWord);
                    key.SetValue("GlobalMaxTcpWindowSize", 0xFFFF, RegistryValueKind.DWord);
                    key.SetValue("MaxConnectionsPer1_0Server", 0x0A, RegistryValueKind.DWord);
                    key.SetValue("MaxConnectionsPerServer", 0x0A, RegistryValueKind.DWord);
                    key.SetValue("MaxFreeTcbs", 0x10000, RegistryValueKind.DWord);
                    key.SetValue("EnableTCPA", 0x00, RegistryValueKind.DWord);
                    key.SetValue("Tcp1323Opts", 0x01, RegistryValueKind.DWord);
                    key.SetValue("TcpCreateAndConnectTcbRateLimitDepth", 0x00, RegistryValueKind.DWord);
                    key.SetValue("TcpMaxDataRetransmissions", 0x03, RegistryValueKind.DWord);
                    key.SetValue("TcpMaxDupAcks", 0x02, RegistryValueKind.DWord);
                    key.SetValue("TcpMaxSendFree", 0xFFFF, RegistryValueKind.DWord);
                    key.SetValue("TcpNumConnections", 0xFFFFFFFE, RegistryValueKind.DWord);
                    key.SetValue("MaxHashTableSize", 0x10000, RegistryValueKind.DWord);
                    key.SetValue("MaxUserPort", 0xFFFE, RegistryValueKind.DWord);
                    key.SetValue("SackOpts", 0x01, RegistryValueKind.DWord);
                    key.SetValue("SynAttackProtect", 0x01, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\MSMQ\Parameters"))
                {
                    key.SetValue("TCPNoDelay", 0x01, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider"))
                {
                    key.SetValue("LocalPriority", 0x04, RegistryValueKind.DWord);
                    key.SetValue("HostsPriority", 0x05, RegistryValueKind.DWord);
                    key.SetValue("DnsPriority", 0x06, RegistryValueKind.DWord);
                    key.SetValue("NetbtPriority", 0x07, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    key.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    key.SetValue("SystemResponsiveness", 0x00, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Psched"))
                {
                    key.SetValue("NonBestEffortLimit", 0x00, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Psched"))
                {
                    key.SetValue("NonBestEffortLimit", 0x00, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters"))
                {
                    key.SetValue("MaxCmds", 0x1E, RegistryValueKind.DWord);
                    key.SetValue("MaxThreads", 0x1E, RegistryValueKind.DWord);
                    key.SetValue("MaxCollectionCount", 0x20, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"))
                {
                    key.SetValue("IRPStackSize", 0x32, RegistryValueKind.DWord);
                    key.SetValue("SizReqBuf", 0x4410, RegistryValueKind.DWord);
                    key.SetValue("Size", 0x03, RegistryValueKind.DWord);
                    key.SetValue("MaxWorkItems", 0x2000, RegistryValueKind.DWord);
                    key.SetValue("MaxMpxCt", 0x800, RegistryValueKind.DWord);
                    key.SetValue("MaxCmds", 0x800, RegistryValueKind.DWord);
                    key.SetValue("DisableStrictNameChecking", 0x01, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void biglatencyreg()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\services\Tcpip\Parameters\Interfaces\{2C7B2EE4-D141-4A1C-97DA-E7C9EC9B9B3F}"))
                {
                    key.SetValue("UseZeroBroadcast", 0x00, RegistryValueKind.DWord);
                    key.SetValue("EnableDeadGWDetect", 0x01, RegistryValueKind.DWord);
                    key.SetValue("EnableDHCP", 0x01, RegistryValueKind.DWord);
                    key.SetValue("Domain", "", RegistryValueKind.String);
                    key.SetValue("RegistrationEnabled", 0x01, RegistryValueKind.DWord);
                    key.SetValue("RegisterAdapterName", 0x00, RegistryValueKind.DWord);
                    key.SetValue("DhcpServer", "192.168.1.1", RegistryValueKind.String);
                    key.SetValue("Lease", 0x0000A8C0, RegistryValueKind.DWord);
                    key.SetValue("LeaseObtainedTime", 0x5784A64B, RegistryValueKind.DWord);
                    key.SetValue("T1", 0x5784FAAB, RegistryValueKind.DWord);
                    key.SetValue("T2", 0x578539F3, RegistryValueKind.DWord);
                    key.SetValue("LeaseTerminatesTime", 0x57854F0B, RegistryValueKind.DWord);
                    key.SetValue("AddressType", 0x00, RegistryValueKind.DWord);
                    key.SetValue("IsServerNapAware", 0x00, RegistryValueKind.DWord);
                    key.SetValue("DhcpConnForceBroadcastFlag", 0x00, RegistryValueKind.DWord);
                    key.SetValue("IPAddress", new byte[] { 0x00, 0x00 }, RegistryValueKind.Binary);
                    key.SetValue("SubnetMask", new byte[] { 0x00, 0x00 }, RegistryValueKind.Binary);
                    key.SetValue("DefaultGateway", new byte[] { 0x00, 0x00 }, RegistryValueKind.Binary);
                    key.SetValue("DefaultGatewayMetric", new byte[] { 0x00, 0x00 }, RegistryValueKind.Binary);
                    key.SetValue("DhcpIPAddress", "192.168.1.36", RegistryValueKind.String);
                    key.SetValue("DhcpSubnetMask", "255.255.255.0", RegistryValueKind.String);
                    key.SetValue("NameServer", "190.202.81.115,192.95.48.17", RegistryValueKind.String);
                    key.SetValue("TCPNoDelay", 0x00630FFF, RegistryValueKind.DWord);
                    key.SetValue("TcpAckFrequency", 0x00630FFF, RegistryValueKind.DWord);
                    key.SetValue("TcpDelAckTicks", 0x00, RegistryValueKind.DWord);
                    key.SetValue("TcpWindowSize", 0x00630FFF, RegistryValueKind.DWord);
                    key.SetValue("MSS", 0x1460, RegistryValueKind.DWord);
                    key.SetValue("MTU", 0x1500, RegistryValueKind.DWord);
                    key.SetValue("DhcpInterfaceOptions", new byte[] {
                        0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x0B, 0x4F, 0x85, 0x57, 0xC0, 0xA8, 0x01, 0x01,
                        0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x0B, 0x4F, 0x85, 0x57, 0xC0, 0xA8, 0x01, 0x01,
                        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x0B, 0x4F, 0x85, 0x57, 0xFF, 0xFF, 0xFF, 0x00,
                        0x36, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x0B, 0x4F, 0x85, 0x57, 0xC0, 0xA8, 0x01, 0x01,
                        0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x0B, 0x4F, 0x85, 0x57, 0x05, 0x00, 0x00, 0x00,
                        0xFC, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x4E, 0xA6, 0x84, 0x57, 0x01, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x4E, 0xA6,
                        0x84, 0x57, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x4F, 0x85, 0x57, 0x47, 0x49,
                        0x47, 0x41, 0x42, 0x59, 0x54, 0x45, 0x2D, 0x50, 0x43, 0x00, 0x33, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x0B, 0x4F, 0x85, 0x57, 0x00, 0x00, 0xA8, 0xC0
                    }, RegistryValueKind.Binary);
                    key.SetValue("DhcpGatewayHardware", new byte[] { 0xC0, 0xA8, 0x01, 0x01, 0x06, 0x00, 0x00, 0x00, 0xB0, 0xC5, 0x54, 0xA7, 0x63, 0xEE }, RegistryValueKind.Binary);
                    key.SetValue("DhcpGatewayHardwareCount", 0x01, RegistryValueKind.DWord);
                    key.SetValue("DhcpNameServer", "192.168.1.1", RegistryValueKind.String);
                    key.SetValue("DhcpDefaultGateway", new byte[] { 0x31, 0x00, 0x39, 0x00, 0x32, 0x00, 0x2E, 0x00, 0x31, 0x00, 0x36, 0x00, 0x38, 0x00, 0x2E, 0x00, 0x31, 0x00, 0x2E, 0x00, 0x31, 0x00, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                    key.SetValue("DhcpSubnetMaskOpt", new byte[] { 0x32, 0x00, 0x35, 0x00, 0x35, 0x00, 0x2E, 0x00, 0x32, 0x00, 0x35, 0x00, 0x35, 0x00, 0x2E, 0x00, 0x32, 0x00, 0x35, 0x00, 0x35, 0x00, 0x2E, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar configurações no registro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void kryptonButton11_Click(object sender, EventArgs e)
        {
            string folderPath = @"C:/Windows\System32\winevt\Logs";
            int deletedFilesCount = 0;

            try
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                string[] directories = Directory.GetDirectories(folderPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedFilesCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"{deletedFilesCount} Deleted Files", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void panel13_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}