# ATM Döviz Çekme Sistemi - Veritabanı ve Backend Katmanı

Bu proje; kullanıcının banka kartı ve PIN doğrulaması ile döviz (USD, EUR, TRY) çekebilmesini sağlayan, kasa banknot stoklarını kontrol eden, 2 aşamalı işlem (ACID Rollback) ile para kaybını önleyen ve çok dilli makbuz üreten profesyonel bir bankamatik backend ve veritabanı çözümüdür.

---

## 📁 Proje Dosya Yapısı

* [database_schema.sql](file:///C:/Users/Pazarlar%20MYO/.gemini/antigravity/scratch/atm_projesi/database_schema.sql): 
  * MS SQL Server / T-SQL şeması.
  * Tablolar, yabancı anahtarlar (FK), Check ve Unique kısıtlamaları.
  * Atomik işlem ve donanım hatasında geri alma mantığı içeren `sp_DovizCek` Stored Procedure.
  * Gerçekçi müşteri, kart (SHA-256 şifreli), hesap ve kasa stok başlangıç verileri (Seed Data).

* [database_schema_sqlite.sql](file:///C:/Users/Pazarlar%20MYO/.gemini/antigravity/scratch/atm_projesi/database_schema_sqlite.sql): 
  * DB Browser for SQLite, Python veya taşınabilir sistemler için ANSI SQL / SQLite uyumlu veritabanı kurulum scripti.

* [AtmBackend.cs](file:///C:/Users/Pazarlar%20MYO/.gemini/antigravity/scratch/atm_projesi/AtmBackend.cs): 
  * C# .NET ile yazılmış nesne yönelimli (OOP) backend motoru.
  * `SecurityService`: SHA-256 PIN hash'leme, kart maskeleme, 3 hatalı denemede blokaj.
  * `CashDispenser`: Açgözlü (Greedy) banknot kupür paylaştırma algoritması (100$, 50$, 20$).
  * `ReceiptService`: Türkçe ve İngilizce işlem makbuzu üreticisi (i18n).
  * `AtmBackendService`: Çekirdek bankacılık simülasyonu, 2-Phase Commit ve Donanım hatasında Rollback.
  * `Program.Main`: Tüm FR ve NFR senaryolarını test eden otomatik test yürütücüsü.

* [AtmBackend.exe](file:///C:/Users/Pazarlar%20MYO/.gemini/antigravity/scratch/atm_projesi/AtmBackend.exe): 
  * Derlenmiş, tek tıkla çalıştırılabilen konsol uygulaması.

---

## 🚀 Nasıl Çalıştırılır ve Yeniden Derlenir?

### 1. Hazır Uygulamayı Çalıştırmak İçin:
PowerShell veya Komut Satırında proje dizinindeyken:
```powershell
.\AtmBackend.exe
```

### 2. C# Kodunu Yeniden Derlemek İçin:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /out:AtmBackend.exe AtmBackend.cs
```

---

## 🧪 Test Edilen Senaryolar
1. **FR-01, FR-02, FR-04:** Kart ve PIN doğrulama, dil seçimi, döviz bakiyeleri ve güncel kurları listeleme.
2. **FR-03, FR-05, FR-06, FR-07:** 270 USD çekme talebi -> Kasa kupür dağıtımı (2x100$ + 1x50$ + 1x20$), bakiye düşümü ve makbuz basımı.
3. **NFR-03 (Güvenilirlik & Rollback):** Nakit dağıtıcı sıkışması simülasyonunda bakiyenin eksiksiz iadesi (sıfır kayıp).
4. **Hata Kontrolleri:** Hesapta olmayan tutarın (10.000 USD) veya ATM kupürüne uymayan tutarın (25 USD) reddedilmesi.
5. **NFR-01 (Güvenlik):** Üst üste 3 kez hatalı PIN girildiğinde kartın otomatik bloke edilmesi.
