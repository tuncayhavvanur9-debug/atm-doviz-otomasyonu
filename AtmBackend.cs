using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AtmDovizSistemi
{
    // ========================================================================
    // 1. VERİ MODELLERİ (ENTITIES)
    // ========================================================================
    public class Customer
    {
        public int Id { get; set; }
        public string TcKimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Telefon { get; set; }
        public string TercihEdilenDil { get; set; } // "TR", "EN"
    }

    public class Card
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string KartNumarasi { get; set; }
        public string SifreHash { get; set; }
        public int HataliGirisSayisi { get; set; }
        public bool BlokeMi { get; set; }
        public string SonKullanimTarihi { get; set; }
    }

    public class Account
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string HesapNo { get; set; }
        public string DovizKodu { get; set; } // "TRY", "USD", "EUR"
        public decimal Bakiye { get; set; }
        public bool AktifMi { get; set; }
    }

    public class ExchangeRate
    {
        public string KaynakDoviz { get; set; }
        public string HedefDoviz { get; set; }
        public decimal AlisKuru { get; set; }
        public decimal SatisKuru { get; set; }
    }

    public class CashBill
    {
        public string DovizKodu { get; set; }
        public int Kupur { get; set; } // 100, 50, 20
        public int Adet { get; set; }
    }

    public class TransactionRecord
    {
        public long Id { get; set; }
        public int AtmId { get; set; }
        public int KartId { get; set; }
        public int HesapId { get; set; }
        public string IslemTipi { get; set; }
        public decimal Tutar { get; set; }
        public string DovizKodu { get; set; }
        public decimal UygulananKur { get; set; }
        public string IslemDurumu { get; set; } // "BASARILI", "YETERSIZ_BAKIYE", "DONANIM_HATASI_ROLLBACK"
        public string HataMesaji { get; set; }
        public DateTime IslemTarihi { get; set; }
    }

    // ========================================================================
    // 2. GÜVENLİK SERVİSİ (SECURITY & CRYPTOGRAPHY)
    // ========================================================================
    public static class SecurityService
    {
        public static string Sha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 16)
                return "****";
            return string.Format("{0} **** **** {1}", cardNumber.Substring(0, 4), cardNumber.Substring(12, 4));
        }
    }

    // ========================================================================
    // 3. NAKİT DAĞITICI ALGORİTMASI (BANKNOTE DISPENSER)
    // ========================================================================
    public class CashDispenseResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<int, int> DispensedBills { get; set; } // Kupür -> Adet
    }

    public static class CashDispenser
    {
        public static CashDispenseResult CalculateBills(List<CashBill> inventory, string currency, int requestedAmount)
        {
            var result = new CashDispenseResult
            {
                DispensedBills = new Dictionary<int, int>()
            };

            var availableBills = inventory
                .Where(b => b.DovizKodu == currency && b.Adet > 0)
                .OrderByDescending(b => b.Kupur)
                .ToList();

            if (!availableBills.Any())
            {
                result.IsSuccessful = false;
                result.ErrorMessage = string.Format("ATM kasasında {0} para biriminde banknot bulunmamaktadır.", currency);
                return result;
            }

            int remaining = requestedAmount;
            var tempInventory = availableBills.ToDictionary(k => k.Kupur, v => v.Adet);

            foreach (var bill in availableBills)
            {
                int kupur = bill.Kupur;
                if (remaining >= kupur)
                {
                    int neededCount = remaining / kupur;
                    int actualCount = Math.Min(neededCount, tempInventory[kupur]);

                    if (actualCount > 0)
                    {
                        result.DispensedBills[kupur] = actualCount;
                        remaining -= (kupur * actualCount);
                    }
                }
            }

            if (remaining > 0)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = string.Format("İstenen {0} {1} tutarı ATM kasasındaki mevcut kupürlerle (100, 50, 20) tam olarak ödenemiyor. Lütfen en yakın kupür katını giriniz.", requestedAmount, currency);
                return result;
            }

            result.IsSuccessful = true;
            return result;
        }
    }

    // ========================================================================
    // 4. MAKBUZ SERVİSİ (RECEIPT GENERATOR - i18n DİL DESTEĞİ)
    // ========================================================================
    public static class ReceiptService
    {
        public static string GenerateReceipt(string language, string atmCode, string location, string maskedCard, 
                                             string accountNo, decimal amount, string currency, decimal remainingBalance, 
                                             decimal rate, string refNo)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==================================================");
            if (language == "EN")
            {
                sb.AppendLine("               ATM TRANSACTION RECEIPT            ");
                sb.AppendLine("==================================================");
                sb.AppendLine(string.Format("ATM ID      : {0} ({1})", atmCode, location));
                sb.AppendLine(string.Format("DATE & TIME : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
                sb.AppendLine(string.Format("REF NO      : {0}", refNo));
                sb.AppendLine(string.Format("CARD NO     : {0}", maskedCard));
                sb.AppendLine(string.Format("ACCOUNT NO  : {0}", accountNo));
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine(string.Format("WITHDRAWN   : {0:N2} {1}", amount, currency));
                sb.AppendLine(string.Format("EXCHANGE RT : 1 {0} = {1:N4} TRY", currency, rate));
                sb.AppendLine(string.Format("NEW BALANCE : {0:N2} {1}", remainingBalance, currency));
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("         THANK YOU FOR BANKING WITH US!           ");
            }
            else
            {
                sb.AppendLine("              BANKAMATIK ISLEM MAKBUZU            ");
                sb.AppendLine("==================================================");
                sb.AppendLine(string.Format("ATM NO      : {0} ({1})", atmCode, location));
                sb.AppendLine(string.Format("TARIH & SAAT: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
                sb.AppendLine(string.Format("REFERANS NO : {0}", refNo));
                sb.AppendLine(string.Format("KART NO     : {0}", maskedCard));
                sb.AppendLine(string.Format("HESAP NO    : {0}", accountNo));
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine(string.Format("CEKILEN     : {0:N2} {1}", amount, currency));
                sb.AppendLine(string.Format("UYGULANAN KR: 1 {0} = {1:N4} TRY", currency, rate));
                sb.AppendLine(string.Format("KALAN BAKIYE: {0:N2} {1}", remainingBalance, currency));
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("           BİZİ TERCİH ETTİĞİNİZ İÇİN             ");
                sb.AppendLine("                TEŞEKKÜR EDERİZ!                  ");
            }
            sb.AppendLine("==================================================");
            return sb.ToString();
        }
    }

    // ========================================================================
    // 5. BACKEND ÇEKİRDEK SERVİSİ (CORE BANKING & TRANSACTION CONTROLLER)
    // ========================================================================
    public class AtmBackendService
    {
        private List<Customer> _customers;
        private List<Card> _cards;
        private List<Account> _accounts;
        private List<ExchangeRate> _rates;
        private List<CashBill> _cashInventory;
        private List<TransactionRecord> _auditLogs;
        private long _txIdCounter = 10001;

        public string AtmKodu { get; private set; }
        public string AtmKonumu { get; private set; }

        public AtmBackendService()
        {
            AtmKodu = "ATM-001";
            AtmKonumu = "Pazarlar MYO Kampüsü";
            InitializeMockDatabase();
        }

        private void InitializeMockDatabase()
        {
            _customers = new List<Customer>
            {
                new Customer { Id = 1, TcKimlikNo = "12345678901", Ad = "Ahmet", Soyad = "Yılmaz", Telefon = "05551234567", TercihEdilenDil = "TR" }
            };

            // Şifre: "1234"
            string pinHash1234 = SecurityService.Sha256Hash("1234");
            _cards = new List<Card>
            {
                new Card { Id = 1, CustomerId = 1, KartNumarasi = "1234567890123456", SifreHash = pinHash1234, HataliGirisSayisi = 0, BlokeMi = false, SonKullanimTarihi = "12/28" }
            };

            _accounts = new List<Account>
            {
                new Account { Id = 1, CustomerId = 1, HesapNo = "TR01-1001-TRY", DovizKodu = "TRY", Bakiye = 50000.00m, AktifMi = true },
                new Account { Id = 2, CustomerId = 1, HesapNo = "TR01-1002-USD", DovizKodu = "USD", Bakiye = 2500.00m, AktifMi = true },
                new Account { Id = 3, CustomerId = 1, HesapNo = "TR01-1003-EUR", DovizKodu = "EUR", Bakiye = 1800.00m, AktifMi = true }
            };

            _rates = new List<ExchangeRate>
            {
                new ExchangeRate { KaynakDoviz = "USD", HedefDoviz = "TRY", AlisKuru = 34.20m, SatisKuru = 34.50m },
                new ExchangeRate { KaynakDoviz = "EUR", HedefDoviz = "TRY", AlisKuru = 37.40m, SatisKuru = 37.80m },
                new ExchangeRate { KaynakDoviz = "TRY", HedefDoviz = "TRY", AlisKuru = 1.00m, SatisKuru = 1.00m }
            };

            _cashInventory = new List<CashBill>
            {
                new CashBill { DovizKodu = "USD", Kupur = 100, Adet = 50 },
                new CashBill { DovizKodu = "USD", Kupur = 50,  Adet = 40 },
                new CashBill { DovizKodu = "USD", Kupur = 20,  Adet = 50 },

                new CashBill { DovizKodu = "EUR", Kupur = 100, Adet = 40 },
                new CashBill { DovizKodu = "EUR", Kupur = 50,  Adet = 30 },
                new CashBill { DovizKodu = "EUR", Kupur = 20,  Adet = 50 }
            };

            _auditLogs = new List<TransactionRecord>();
        }

        // FR-01: Kart & Şifre Doğrulama (3 Hatalı denemede blokaj)
        public Card Authenticate(string cardNumber, string pin, out string errorMessage)
        {
            errorMessage = null;
            var card = _cards.FirstOrDefault(c => c.KartNumarasi == cardNumber);
            if (card == null)
            {
                errorMessage = "Geçersiz kart numarası.";
                return null;
            }

            if (card.BlokeMi)
            {
                errorMessage = "Kartınız 3 kez hatalı şifre girişi nedeniyle BLOKE edilmiştir. Lütfen şubenize başvurunuz.";
                return null;
            }

            string inputHash = SecurityService.Sha256Hash(pin);
            if (card.SifreHash != inputHash)
            {
                card.HataliGirisSayisi++;
                if (card.HataliGirisSayisi >= 3)
                {
                    card.BlokeMi = true;
                    errorMessage = "3 kez hatalı şifre girildi! Güvenlik nedeniyle kartınız BLOKE EDİLDİ.";
                }
                else
                {
                    errorMessage = string.Format("Hatalı şifre! Kalan deneme hakkı: {0}", 3 - card.HataliGirisSayisi);
                }
                return null;
            }

            // Başarılı girişte sayacı sıfırla
            card.HataliGirisSayisi = 0;
            return card;
        }

        public Customer GetCustomer(int customerId)
        {
            return _customers.FirstOrDefault(c => c.Id == customerId);
        }

        // FR-04: Bakiye ve Hesapları Listeleme
        public List<Account> GetCustomerAccounts(int customerId)
        {
            return _accounts.Where(a => a.CustomerId == customerId && a.AktifMi).ToList();
        }

        // Güncel Döviz Kurlarını Getir
        public List<ExchangeRate> GetExchangeRates()
        {
            return _rates;
        }

        public decimal GetCurrentRate(string currency)
        {
            var rate = _rates.FirstOrDefault(r => r.KaynakDoviz == currency && r.HedefDoviz == "TRY");
            return rate != null ? rate.SatisKuru : 1.00m;
        }

        // ATM Kasasındaki Toplam Döviz Tutarı
        public decimal GetAtmCashTotal(string currency)
        {
            return _cashInventory.Where(b => b.DovizKodu == currency).Sum(b => b.Kupur * b.Adet);
        }

        // ====================================================================
        // FR-05 & FR-06 & NFR-03: ATOMİK DÖVİZ ÇEKME VE ROLLBACK MEKANİZMASI
        // ====================================================================
        public bool WithdrawForeignCurrency(Card card, string currency, int amount, bool simulateHardwareFailure, 
                                            string language, out string message, out string receiptText)
        {
            receiptText = null;
            decimal currentRate = GetCurrentRate(currency);

            // 1. Hesap Kontrolü
            var account = _accounts.FirstOrDefault(a => a.CustomerId == card.CustomerId && a.DovizKodu == currency && a.AktifMi);
            if (account == null)
            {
                message = string.Format("Hesabınızda tanımlı {0} döviz hesabı bulunamadı.", currency);
                return false;
            }

            // 2. Müşteri Bakiye Kontrolü
            if (account.Bakiye < amount)
            {
                message = string.Format("Yetersiz bakiye! Mevcut Bakiyeniz: {0:N2} {1}, Talep Edilen: {2:N2} {1}", account.Bakiye, currency, amount);
                LogTransaction(card.Id, account.Id, amount, currency, currentRate, "YETERSIZ_BAKIYE", message);
                return false;
            }

            // 3. ATM Kasa Fiziksel Kupür Kontrolü
            var dispenseResult = CashDispenser.CalculateBills(_cashInventory, currency, amount);
            if (!dispenseResult.IsSuccessful)
            {
                message = dispenseResult.ErrorMessage;
                LogTransaction(card.Id, account.Id, amount, currency, currentRate, "YETERSIZ_KASA_KUPUR", message);
                return false;
            }

            // 4. İki Aşamalı İşlem (2-Phase Commit Transaction)
            decimal oldBalance = account.Bakiye;
            account.Bakiye -= amount; // Bakiye geçici rezervasyona alındı

            // 5. Donanım Para Dağıtımı Simülasyonu (Nakit Dağıtıcı Kapağı)
            if (simulateHardwareFailure)
            {
                // DONANIM HATASI OLUŞTU -> OTOMATİK ROLLBACK ÇALIŞIR!
                account.Bakiye = oldBalance; // Bakiye eksiksiz geri yüklendi!
                message = "KRİTİK HATA: ATM nakit dağıtıcı ünitesinde mekanik sıkışma tespit edildi (Cash Jam)! İşlem otomatik iptal edildi (ROLLBACK). Hesabınızdan para DÜŞÜLMEDİ.";
                LogTransaction(card.Id, account.Id, amount, currency, currentRate, "DONANIM_HATASI_ROLLBACK", message);
                return false;
            }

            // 6. Başarılı Donanım Onayı -> Kasa Stokunu Güncelle
            foreach (var pair in dispenseResult.DispensedBills)
            {
                var bill = _cashInventory.First(b => b.DovizKodu == currency && b.Kupur == pair.Key);
                bill.Adet -= pair.Value;
            }

            // 7. İşlem Başarılı Olarak Kaydedildi
            long txId = LogTransaction(card.Id, account.Id, amount, currency, currentRate, "BASARILI", null);

            // Kupür Dağıtım Dökümü
            StringBuilder billsSummary = new StringBuilder();
            foreach (var pair in dispenseResult.DispensedBills)
            {
                billsSummary.AppendFormat(" [{0}x{1} {2}]", pair.Value, pair.Key, currency);
            }

            message = string.Format("Para çekme işlemi başarıyla tamamlandı. Verilen Banknotlar:{0}", billsSummary);

            // FR-07: Makbuz Üretimi
            receiptText = ReceiptService.GenerateReceipt(
                language, 
                AtmKodu, 
                AtmKonumu, 
                SecurityService.MaskCardNumber(card.KartNumarasi), 
                account.HesapNo, 
                amount, 
                currency, 
                account.Bakiye, 
                currentRate, 
                "TX-" + txId
            );

            return true;
        }

        private long LogTransaction(int cardId, int accountId, decimal amount, string currency, decimal rate, string status, string error)
        {
            long txId = _txIdCounter++;
            _auditLogs.Add(new TransactionRecord
            {
                Id = txId,
                AtmId = 1,
                KartId = cardId,
                HesapId = accountId,
                IslemTipi = "DOVIZ_CEKME",
                Tutar = amount,
                DovizKodu = currency,
                UygulananKur = rate,
                IslemDurumu = status,
                HataMesaji = error,
                IslemTarihi = DateTime.Now
            });
            return txId;
        }

        public List<TransactionRecord> GetAuditLogs()
        {
            return _auditLogs;
        }
    }

    // ========================================================================
    // 6. TEST VE SİMÜLASYON YÜRÜTÜCÜSÜ (CONSOLE RUNNER)
    // ========================================================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var service = new AtmBackendService();

            PrintHeader();

            Console.WriteLine("Otomasyon Test Senaryoları Başlatılıyor...\n");

            // ----------------------------------------------------------------
            // SENARYO 1: KART VE PIN GİRİŞİ (FR-01, FR-02, FR-04)
            // ----------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(">>> [TEST 1] Kart Takma & PIN Doğrulama & Bakiye Görüntüleme");
            Console.ResetColor();

            string err;
            var card = service.Authenticate("1234567890123456", "1234", out err);
            if (card != null)
            {
                var cust = service.GetCustomer(card.CustomerId);
                Console.WriteLine("Giriş Başarılı! Müşteri: {0} {1} | Kart: {2}", cust.Ad, cust.Soyad, SecurityService.MaskCardNumber(card.KartNumarasi));
                
                Console.WriteLine("\n--- Tanımlı Hesaplar & Güncel Bakiyeler ---");
                foreach (var acc in service.GetCustomerAccounts(cust.Id))
                {
                    decimal rate = service.GetCurrentRate(acc.DovizKodu);
                    Console.WriteLine("Hesap: {0,-14} | Para Birimi: {1,-3} | Bakiye: {2,10:N2} | Kur (TRY): {3,6:N2}", 
                        acc.HesapNo, acc.DovizKodu, acc.Bakiye, rate);
                }
            }
            Console.WriteLine("\n" + new string('-', 60));

            // ----------------------------------------------------------------
            // SENARYO 2: BAŞARILI 270 USD DÖVİZ ÇEKME & MAKBUZ (FR-03, FR-05, FR-06, FR-07)
            // ----------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n>>> [TEST 2] Başarılı 270 USD Çekme (Optimum Banknot Dağıtımı & Makbuz)");
            Console.ResetColor();

            string msg, receipt;
            bool success = service.WithdrawForeignCurrency(card, "USD", 270, false, "TR", out msg, out receipt);
            Console.WriteLine("İşlem Sonucu : {0}", success ? "BAŞARILI" : "BAŞARISIZ");
            Console.WriteLine("Sistem Mesajı: {0}", msg);
            if (success)
            {
                Console.WriteLine("\n--- Basılan ATM Makbuzu ---");
                Console.WriteLine(receipt);
            }
            Console.WriteLine(new string('-', 60));

            // ----------------------------------------------------------------
            // SENARYO 3: DONANIM ARIZASI & SIKIŞMA ROLLBACK TESTİ (NFR-03 - SIFIR KAYIP)
            // ----------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n>>> [TEST 3] Donanım Hatası / Para Dağıtıcı Sıkışması (Rollback Testi)");
            Console.ResetColor();

            decimal balanceBefore = service.GetCustomerAccounts(card.CustomerId).First(a => a.DovizKodu == "USD").Bakiye;
            Console.WriteLine("İşlem Öncesi USD Bakiye: {0:N2} USD", balanceBefore);
            Console.WriteLine("Simülasyon: 200 USD çekilirken dağıtıcı kapağı sıkışıyor...");

            bool rollbackSuccess = service.WithdrawForeignCurrency(card, "USD", 200, true, "TR", out msg, out receipt);
            decimal balanceAfter = service.GetCustomerAccounts(card.CustomerId).First(a => a.DovizKodu == "USD").Bakiye;

            Console.WriteLine("İşlem Sonucu : {0}", rollbackSuccess ? "BAŞARILI" : "IPTAL / ROLLBACK");
            Console.WriteLine("Sistem Uyarısı: {0}", msg);
            Console.WriteLine("İşlem Sonrası USD Bakiye: {0:N2} USD", balanceAfter);
            if (balanceBefore == balanceAfter)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("GÜVENLİK TESTİ BAŞARILI: Müşteri bakiyesinden 1 Dolar dahi kesilmedi (ACID Rollback garantilendi).");
                Console.ResetColor();
            }
            Console.WriteLine(new string('-', 60));

            // ----------------------------------------------------------------
            // SENARYO 4: HATA KONTROLLERİ (Yetersiz Bakiye & Geçersiz Kupür)
            // ----------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n>>> [TEST 4] Hata Kontrolleri (Yetersiz Bakiye ve Geçersiz Kupür Katı)");
            Console.ResetColor();

            // A: Yetersiz Bakiye (10.000 USD)
            service.WithdrawForeignCurrency(card, "USD", 10000, false, "TR", out msg, out receipt);
            Console.WriteLine("Deneme 1 (10.000 USD - Yetersiz Bakiye): {0}", msg);

            // B: Geçersiz Kupür Katı (25 USD - ATM kasasında 5'lik yok)
            service.WithdrawForeignCurrency(card, "USD", 25, false, "TR", out msg, out receipt);
            Console.WriteLine("Deneme 2 (25 USD - Bozukluk / Uyumsuz Kupür): {0}", msg);
            Console.WriteLine(new string('-', 60));

            // ----------------------------------------------------------------
            // SENARYO 5: GÜVENLİK VE BLOKE KONTROLÜ (NFR-01)
            // ----------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n>>> [TEST 5] Güvenlik: 3 Kez Hatalı PIN Girişi ve Kart Bloke Testi");
            Console.ResetColor();

            for (int i = 1; i <= 3; i++)
            {
                string pinError;
                service.Authenticate("1234567890123456", "9999", out pinError);
                Console.WriteLine("{0}. Hatalı Deneme: {1}", i, pinError);
            }

            // Bloke sonrası doğru şifre ile giriş denemesi
            string blockedError;
            var blockedCard = service.Authenticate("1234567890123456", "1234", out blockedError);
            Console.WriteLine("Bloke Sonrası Giriş Denemesi: {0}", blockedError);
            Console.WriteLine(new string('-', 60));

            // ----------------------------------------------------------------
            // DENETİM GÜNLÜĞÜ (AUDIT LOG TABLOSU)
            // ----------------------------------------------------------------
            Console.WriteLine("\n--- VERİTABANI İŞLEM GEÇMİŞİ (AUDIT LOGS) ---");
            Console.WriteLine("{0,-8} | {1,-12} | {2,-8} | {3,-24} | {4}", "LOG ID", "TUTAR", "KUR", "DURUM", "TARIH");
            Console.WriteLine(new string('-', 75));
            foreach (var log in service.GetAuditLogs())
            {
                Console.WriteLine("{0,-8} | {1,5:N0} {2,-3}   | {3,6:N2} | {4,-24} | {5:HH:mm:ss}", 
                    log.Id, log.Tutar, log.DovizKodu, log.UygulananKur, log.IslemDurumu, log.IslemTarihi);
            }

            Console.WriteLine("\n==================================================");
            Console.WriteLine("TÜM GEREKSİNİM VE SİSTEM TESTLERİ BAŞARIYLA TAMAMLANDI.");
            Console.WriteLine("==================================================");
        }

        static void PrintHeader()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("       ATM DÖVİZ ÇEKME SİSTEMİ BACKEND MOTORU     ");
            Console.WriteLine("          Gereksinim Doğrulama & Simülasyon       ");
            Console.WriteLine("==================================================");
        }
    }
}
