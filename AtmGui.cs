using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace AtmMasaustu
{
    public class AtmForm : Form
    {
        // Durum Değişkenleri
        private string _language = "TR";
        private string _step = "WELCOME"; // WELCOME, PIN, MENU, AMOUNT
        private string _currentPin = "";
        private int _failedAttempts = 0;
        private string _selectedCurrency = "USD";
        private decimal _balanceTry = 50000.00m;
        private decimal _balanceUsd = 2500.00m;
        private decimal _balanceEur = 1800.00m;
        private decimal _rateUsd = 34.50m;
        private decimal _rateEur = 37.80m;

        // Kasa Stokları
        private Dictionary<int, int> _usdInventory = new Dictionary<int, int> { { 100, 50 }, { 50, 40 }, { 20, 50 } };
        private Dictionary<int, int> _eurInventory = new Dictionary<int, int> { { 100, 40 }, { 50, 30 }, { 20, 50 } };

        // GUI Bileşenleri
        private Panel pnlHeader;
        private Label lblTitle;
        private Panel pnlScreen;
        private Panel pnlHardware;
        private Label lblDispenser;
        private TextBox txtReceipt;
        private CheckBox chkHardwareError;
        private Button btnCardAction;
        private bool _isCardInserted = false;

        public AtmForm()
        {
            this.Text = "Bankamatik Döviz Çekme Simülatörü - Pazarlar MYO";
            this.Size = new Size(820, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(24, 30, 44);

            InitializeComponents();
            RenderScreen();
        }

        private void InitializeComponents()
        {
            // Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            lblTitle = new Label
            {
                Text = "PAZARLAR BANK | ATM-001 (DÖVİZ ÇEKME SİSTEMİ)",
                ForeColor = Color.Cyan,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 14),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            // Sağ Donanım Paneli
            pnlHardware = new Panel
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15)
            };

            Label lblHwTitle = new Label
            {
                Text = "DONANIM & MAKBUZ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(15, 15),
                AutoSize = true
            };
            pnlHardware.Controls.Add(lblHwTitle);

            btnCardAction = new Button
            {
                Text = "💳 Kartı Tak (Ahmet Yılmaz)",
                Size = new Size(230, 40),
                Location = new Point(15, 45),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnCardAction.Click += (s, e) => ToggleCard();
            pnlHardware.Controls.Add(btnCardAction);

            chkHardwareError = new CheckBox
            {
                Text = "Mekanik Para Sıkışması Simüle Et (Rollback)",
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 8),
                Location = new Point(15, 95),
                Size = new Size(230, 40)
            };
            pnlHardware.Controls.Add(chkHardwareError);

            Label lblReceiptTitle = new Label
            {
                Text = "ATM Makbuz Çıktısı:",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(15, 140),
                AutoSize = true
            };
            pnlHardware.Controls.Add(lblReceiptTitle);

            txtReceipt = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Location = new Point(15, 165),
                Size = new Size(230, 360),
                BackColor = Color.FromArgb(254, 252, 232),
                ForeColor = Color.Black,
                Font = new Font("Consolas", 8),
                ScrollBars = ScrollBars.Vertical
            };
            pnlHardware.Controls.Add(txtReceipt);

            // Alt Nakit Dağıtıcı Bölmesi
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            lblDispenser = new Label
            {
                Text = "💵 NAKİT BÖLMESİ: Kapak Kapalı (Para Yok)",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, 22),
                AutoSize = true
            };
            pnlBottom.Controls.Add(lblDispenser);

            // Ana ATM Dokunmatik Ekran Paneli
            pnlScreen = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(11, 24, 48),
                Padding = new Padding(20)
            };

            this.Controls.Add(pnlScreen);
            this.Controls.Add(pnlHardware);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
        }

        private void ToggleCard()
        {
            if (!_isCardInserted)
            {
                _isCardInserted = true;
                btnCardAction.Text = "⏏ Kartı Çıkar (Eject)";
                btnCardAction.BackColor = Color.FromArgb(220, 38, 38);
                _currentPin = "";
                _step = "PIN";
            }
            else
            {
                _isCardInserted = false;
                btnCardAction.Text = "💳 Kartı Tak (Ahmet Yılmaz)";
                btnCardAction.BackColor = Color.FromArgb(14, 165, 233);
                _step = "WELCOME";
                lblDispenser.Text = "💵 NAKİT BÖLMESİ: Kapak Kapalı (Para Yok)";
                lblDispenser.ForeColor = Color.Gray;
            }
            RenderScreen();
        }

        private void RenderScreen()
        {
            pnlScreen.Controls.Clear();

            if (_step == "WELCOME")
            {
                Label lblWelcome = new Label
                {
                    Text = "LÜTFEN İŞLEM YAPMAK İÇİN KARTINIZI TAKINIZ",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 13, FontStyle.Bold),
                    Location = new Point(40, 60),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblWelcome);

                Label lblSub = new Label
                {
                    Text = "Sağ panelden 'Kartı Tak' butonuna tıklayarak işlemi başlatabilirsiniz.\n\nGüncel Kurlar:\n1 USD = 34.50 TRY\n1 EUR = 37.80 TRY",
                    ForeColor = Color.SkyBlue,
                    Font = new Font("Segoe UI", 10),
                    Location = new Point(40, 110),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblSub);
            }
            else if (_step == "PIN")
            {
                Label lblPinPrompt = new Label
                {
                    Text = "LÜTFEN 4 HANELİ ŞİFRENİZİ GİRİNİZ (Varsayılan: 1234):",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Location = new Point(30, 20),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblPinPrompt);

                Label lblPinDisplay = new Label
                {
                    Text = new string('●', _currentPin.Length).PadRight(4, '○'),
                    ForeColor = Color.Cyan,
                    Font = new Font("Segoe UI", 24, FontStyle.Bold),
                    Location = new Point(30, 55),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblPinDisplay);

                // Tuş Takımı
                int startX = 30, startY = 110, size = 55;
                for (int i = 1; i <= 9; i++)
                {
                    int row = (i - 1) / 3;
                    int col = (i - 1) % 3;
                    Button btn = new Button
                    {
                        Text = i.ToString(),
                        Size = new Size(size, size),
                        Location = new Point(startX + col * (size + 10), startY + row * (size + 10)),
                        BackColor = Color.FromArgb(30, 41, 59),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 12, FontStyle.Bold),
                        FlatStyle = FlatStyle.Flat
                    };
                    string num = i.ToString();
                    btn.Click += (s, e) => AddPinDigit(num);
                    pnlScreen.Controls.Add(btn);
                }

                Button btnClear = new Button
                {
                    Text = "Sil",
                    Size = new Size(size, size),
                    Location = new Point(startX, startY + 3 * (size + 10)),
                    BackColor = Color.FromArgb(180, 83, 9),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                btnClear.Click += (s, e) => { _currentPin = ""; RenderScreen(); };
                pnlScreen.Controls.Add(btnClear);

                Button btnZero = new Button
                {
                    Text = "0",
                    Size = new Size(size, size),
                    Location = new Point(startX + (size + 10), startY + 3 * (size + 10)),
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                btnZero.Click += (s, e) => AddPinDigit("0");
                pnlScreen.Controls.Add(btnZero);

                Button btnConfirm = new Button
                {
                    Text = "Giriş",
                    Size = new Size(size, size),
                    Location = new Point(startX + 2 * (size + 10), startY + 3 * (size + 10)),
                    BackColor = Color.FromArgb(16, 185, 129),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                btnConfirm.Click += (s, e) => CheckPin();
                pnlScreen.Controls.Add(btnConfirm);
            }
            else if (_step == "MENU")
            {
                Label lblGreeting = new Label
                {
                    Text = "HOŞ GELDİNİZ, SAYIN AHMET YILMAZ",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Location = new Point(30, 20),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblGreeting);

                Label lblBalances = new Label
                {
                    Text = string.Format("TL Bakiye : {0:N2} ₺\nUSD Bakiye: {1:N2} $ (Kur: {2:N2} ₺)\nEUR Bakiye: {3:N2} € (Kur: {4:N2} ₺)",
                        _balanceTry, _balanceUsd, _rateUsd, _balanceEur, _rateEur),
                    ForeColor = Color.LightGreen,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Location = new Point(30, 55),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblBalances);

                Button btnUsd = new Button
                {
                    Text = "💵 USD ($) ÇEK",
                    Size = new Size(200, 50),
                    Location = new Point(30, 140),
                    BackColor = Color.FromArgb(5, 150, 105),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                btnUsd.Click += (s, e) => { _selectedCurrency = "USD"; _step = "AMOUNT"; RenderScreen(); };
                pnlScreen.Controls.Add(btnUsd);

                Button btnEur = new Button
                {
                    Text = "💶 EUR (€) ÇEK",
                    Size = new Size(200, 50),
                    Location = new Point(250, 140),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                btnEur.Click += (s, e) => { _selectedCurrency = "EUR"; _step = "AMOUNT"; RenderScreen(); };
                pnlScreen.Controls.Add(btnEur);
            }
            else if (_step == "AMOUNT")
            {
                Label lblTitle = new Label
                {
                    Text = string.Format("ÇEKİLECEK TUTARI SEÇİNİZ ({0}):", _selectedCurrency),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Location = new Point(30, 20),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblTitle);

                decimal currentBal = _selectedCurrency == "USD" ? _balanceUsd : _balanceEur;
                Label lblCurrBal = new Label
                {
                    Text = string.Format("Mevcut Bakiye: {0:N2} {1}", currentBal, _selectedCurrency),
                    ForeColor = Color.SkyBlue,
                    Font = new Font("Segoe UI", 9),
                    Location = new Point(30, 50),
                    AutoSize = true
                };
                pnlScreen.Controls.Add(lblCurrBal);

                int[] fastAmounts = new int[] { 50, 100, 200, 270, 500 };
                int yPos = 85;
                foreach (int amt in fastAmounts)
                {
                    Button btnAmt = new Button
                    {
                        Text = string.Format("{0} {1}", amt, _selectedCurrency),
                        Size = new Size(180, 40),
                        Location = new Point(30, yPos),
                        BackColor = Color.FromArgb(30, 41, 59),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        FlatStyle = FlatStyle.Flat
                    };
                    int currentAmount = amt;
                    btnAmt.Click += (s, e) => ExecuteWithdrawal(currentAmount);
                    pnlScreen.Controls.Add(btnAmt);
                    yPos += 48;
                }

                Button btnBack = new Button
                {
                    Text = "← Geri",
                    Size = new Size(100, 35),
                    Location = new Point(30, 340),
                    BackColor = Color.FromArgb(71, 85, 105),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnBack.Click += (s, e) => { _step = "MENU"; RenderScreen(); };
                pnlScreen.Controls.Add(btnBack);
            }
        }

        private void AddPinDigit(string digit)
        {
            if (_currentPin.Length < 4)
            {
                _currentPin += digit;
                RenderScreen();
                if (_currentPin.Length == 4)
                {
                    CheckPin();
                }
            }
        }

        private void CheckPin()
        {
            if (_currentPin == "1234")
            {
                _failedAttempts = 0;
                _step = "MENU";
                RenderScreen();
            }
            else
            {
                _failedAttempts++;
                _currentPin = "";
                if (_failedAttempts >= 3)
                {
                    MessageBox.Show("3 KEZ HATALI ŞİFRE GİRİLDİ!\nKartınız güvenlik nedeniyle BLOKE edildi.", "KART BLOKE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ToggleCard();
                }
                else
                {
                    MessageBox.Show(string.Format("Hatalı şifre!\nKalan deneme hakkı: {0}", 3 - _failedAttempts), "HATA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    RenderScreen();
                }
            }
        }

        private void ExecuteWithdrawal(int amount)
        {
            decimal currentBal = _selectedCurrency == "USD" ? _balanceUsd : _balanceEur;

            // 1. Bakiye Kontrolü
            if (currentBal < amount)
            {
                MessageBox.Show(string.Format("Yetersiz Bakiye!\nHesabınızda {0:N2} {1} bulunmaktadır.", currentBal, _selectedCurrency), "YETERSİZ BAKİYE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Kasa Banknot Kupür Dağıtımı (100, 50, 20)
            int remaining = amount;
            int c100 = 0, c50 = 0, c20 = 0;

            if (remaining >= 100) { c100 = remaining / 100; remaining %= 100; }
            if (remaining >= 50)  { c50 = remaining / 50;   remaining %= 50; }
            if (remaining >= 20)  { c20 = remaining / 20;   remaining %= 20; }

            if (remaining > 0)
            {
                MessageBox.Show("İstenen tutar ATM banknotlarına (100, 50, 20) uygun değildir!", "KUPÜR HATASI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Donanım Hatası / Para Dağıtıcı Sıkışması Testi (Rollback)
            if (chkHardwareError.Checked)
            {
                MessageBox.Show("KRİTİK HATA: Para dağıtıcı ünitesinde mekanik sıkışma tespit edildi (Cash Jam)!\n\nACID ROLLBACK DEVREDE: Hesabınızdan para DÜŞÜLMEDİ.", "DONANIM HATASI / ROLLBACK", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 4. Başarılı İşlem (Bakiye Güncelleme & Kasa Düşümü)
            if (_selectedCurrency == "USD") _balanceUsd -= amount;
            else _balanceEur -= amount;

            // 5. Nakit Bölmesini Güncelle
            lblDispenser.Text = string.Format("💵 NAKİT VERİLDİ: {0} {1} -> Dağıtım: [{2}x100, {3}x50, {4}x20]", 
                amount, _selectedCurrency, c100, c50, c20);
            lblDispenser.ForeColor = Color.LimeGreen;

            // 6. Makbuz Bas
            decimal rate = _selectedCurrency == "USD" ? _rateUsd : _rateEur;
            decimal newBal = _selectedCurrency == "USD" ? _balanceUsd : _balanceEur;

            txtReceipt.Text = string.Format(
                "==============================\r\n" +
                "       PAZARLAR BANK ATM      \r\n" +
                "==============================\r\n" +
                "TARIH: {0:yyyy-MM-dd HH:mm}\r\n" +
                "KART : 1234 **** **** 3456\r\n" +
                "HESAP: TR01-1002-{1}\r\n" +
                "------------------------------\r\n" +
                "CEKILEN     : {2:N2} {1}\r\n" +
                "UYGULANAN KR: 1 {1} = {3:N2} TL\r\n" +
                "KALAN BAKIYE: {4:N2} {1}\r\n" +
                "------------------------------\r\n" +
                "  BİZİ TERCİH ETTİĞİNİZ İÇİN  \r\n" +
                "       TEŞEKKÜR EDERİZ!       \r\n" +
                "==============================",
                DateTime.Now, _selectedCurrency, amount, rate, newBal
            );

            MessageBox.Show(string.Format("Para çekme işlemi başarılı!\nLütfen paranızı ve makbuzunuzu alınız.\nKalan Bakiye: {0:N2} {1}", newBal, _selectedCurrency), "BAŞARILI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _step = "MENU";
            RenderScreen();
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AtmForm());
        }
    }
}
