-- ============================================================================
-- PROJE: ATM DÖVİZ ÇEKME SİSTEMİ VERİTABANI TASARIMI (MS SQL SERVER & T-SQL)
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AtmDovizDB')
BEGIN
    CREATE DATABASE AtmDovizDB;
END
GO

USE AtmDovizDB;
GO

-- ----------------------------------------------------------------------------
-- 1. TABLOLARIN TEMİZLENMESİ (Mevcutsa Sırayla Kaldırma)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.IslemGecmisi', 'U') IS NOT NULL DROP TABLE dbo.IslemGecmisi;
IF OBJECT_ID('dbo.AtmKasaStok', 'U') IS NOT NULL DROP TABLE dbo.AtmKasaStok;
IF OBJECT_ID('dbo.Atmler', 'U') IS NOT NULL DROP TABLE dbo.Atmler;
IF OBJECT_ID('dbo.Hesaplar', 'U') IS NOT NULL DROP TABLE dbo.Hesaplar;
IF OBJECT_ID('dbo.Kartlar', 'U') IS NOT NULL DROP TABLE dbo.Kartlar;
IF OBJECT_ID('dbo.Musteriler', 'U') IS NOT NULL DROP TABLE dbo.Musteriler;
IF OBJECT_ID('dbo.DovizKurlari', 'U') IS NOT NULL DROP TABLE dbo.DovizKurlari;
IF OBJECT_ID('dbo.DovizTurleri', 'U') IS NOT NULL DROP TABLE dbo.DovizTurleri;
IF OBJECT_ID('dbo.Diller', 'U') IS NOT NULL DROP TABLE dbo.Diller;
GO

-- ----------------------------------------------------------------------------
-- 2. TEMEL REFERANS TABLOLARI
-- ----------------------------------------------------------------------------

-- Çoklu Dil Desteği
CREATE TABLE dbo.Diller (
    DilKodu VARCHAR(5) PRIMARY KEY,
    DilAdi NVARCHAR(50) NOT NULL
);
GO

-- Döviz Türleri (TRY, USD, EUR vb.)
CREATE TABLE dbo.DovizTurleri (
    DovizKodu VARCHAR(3) PRIMARY KEY, -- 'TRY', 'USD', 'EUR'
    Sembol NVARCHAR(5) NOT NULL,      -- '₺', '$', '€'
    DovizAdi NVARCHAR(50) NOT NULL
);
GO

-- Anlık Döviz Kurları (TRY Bazlı)
CREATE TABLE dbo.DovizKurlari (
    KurId INT IDENTITY(1,1) PRIMARY KEY,
    KaynakDoviz VARCHAR(3) NOT NULL FOREIGN KEY REFERENCES dbo.DovizTurleri(DovizKodu),
    HedefDoviz VARCHAR(3) NOT NULL FOREIGN KEY REFERENCES dbo.DovizTurleri(DovizKodu),
    AlisKuru DECIMAL(18,4) NOT NULL,
    SatisKuru DECIMAL(18,4) NOT NULL,
    GuncellenmeTarihi DATETIME2 DEFAULT SYSDATETIME()
);
GO

-- ----------------------------------------------------------------------------
-- 3. MÜŞTERİ VE HESAP TABLOLARI
-- ----------------------------------------------------------------------------

-- Müşteri Bilgileri
CREATE TABLE dbo.Musteriler (
    MusteriId INT IDENTITY(1,1) PRIMARY KEY,
    TcKimlikNo CHAR(11) UNIQUE NOT NULL,
    Ad NVARCHAR(50) NOT NULL,
    Soyad NVARCHAR(50) NOT NULL,
    Telefon VARCHAR(15) NOT NULL,
    TercihEdilenDil VARCHAR(5) DEFAULT 'TR' FOREIGN KEY REFERENCES dbo.Diller(DilKodu),
    OlusturulmaTarihi DATETIME2 DEFAULT SYSDATETIME()
);
GO

-- Banka Kartları ve Güvenlik
CREATE TABLE dbo.Kartlar (
    KartId INT IDENTITY(1,1) PRIMARY KEY,
    MusteriId INT NOT NULL FOREIGN KEY REFERENCES dbo.Musteriler(MusteriId),
    KartNumarasi CHAR(16) UNIQUE NOT NULL,
    SifreHash CHAR(64) NOT NULL, -- SHA-256 Hash
    HataliGirisSayisi INT DEFAULT 0,
    BlokeMi BIT DEFAULT 0,
    SonKullanimTarihi CHAR(5) NOT NULL -- '12/28'
);
GO

-- Müşteri Hesapları (TL, USD, EUR cüzdanları)
CREATE TABLE dbo.Hesaplar (
    HesapId INT IDENTITY(1,1) PRIMARY KEY,
    MusteriId INT NOT NULL FOREIGN KEY REFERENCES dbo.Musteriler(MusteriId),
    HesapNo VARCHAR(20) UNIQUE NOT NULL,
    DovizKodu VARCHAR(3) NOT NULL FOREIGN KEY REFERENCES dbo.DovizTurleri(DovizKodu),
    Bakiye DECIMAL(18,2) NOT NULL DEFAULT 0.00 CHECK (Bakiye >= 0),
    AktifMi BIT DEFAULT 1
);
GO

-- ----------------------------------------------------------------------------
-- 4. ATM VE FİZİKSEL KASA YÖNETİMİ
-- ----------------------------------------------------------------------------

CREATE TABLE dbo.Atmler (
    AtmId INT IDENTITY(1,1) PRIMARY KEY,
    AtmKodu VARCHAR(20) UNIQUE NOT NULL,
    Konum NVARCHAR(100) NOT NULL,
    AktifMi BIT DEFAULT 1
);
GO

-- ATM İçerisindeki Fiziksel Banknot Stoku (USD/EUR kupürleri)
CREATE TABLE dbo.AtmKasaStok (
    StokId INT IDENTITY(1,1) PRIMARY KEY,
    AtmId INT NOT NULL FOREIGN KEY REFERENCES dbo.Atmler(AtmId),
    DovizKodu VARCHAR(3) NOT NULL FOREIGN KEY REFERENCES dbo.DovizTurleri(DovizKodu),
    KupurDegeri INT NOT NULL, -- 20, 50, 100
    Adet INT NOT NULL CHECK (Adet >= 0),
    CONSTRAINT UQ_AtmKupur UNIQUE (AtmId, DovizKodu, KupurDegeri)
);
GO

-- ----------------------------------------------------------------------------
-- 5. İŞLEM GEÇMİŞİ VE DENETİM (AUDIT LOG)
-- ----------------------------------------------------------------------------

CREATE TABLE dbo.IslemGecmisi (
    IslemId BIGINT IDENTITY(1,1) PRIMARY KEY,
    AtmId INT NOT NULL FOREIGN KEY REFERENCES dbo.Atmler(AtmId),
    KartId INT NOT NULL FOREIGN KEY REFERENCES dbo.Kartlar(KartId),
    HesapId INT NOT NULL FOREIGN KEY REFERENCES dbo.Hesaplar(HesapId),
    IslemTipi VARCHAR(30) NOT NULL, -- 'DOVIZ_CEKME'
    Tutar DECIMAL(18,2) NOT NULL,
    DovizKodu VARCHAR(3) NOT NULL FOREIGN KEY REFERENCES dbo.DovizTurleri(DovizKodu),
    UygulananKur DECIMAL(18,4) NOT NULL,
    IslemDurumu VARCHAR(20) NOT NULL, -- 'BASARILI', 'YETERSIZ_BAKIYE', 'YETERSIZ_KASA', 'DONANIM_HATASI_ROLLBACK'
    HataMesaji NVARCHAR(250) NULL,
    IslemTarihi DATETIME2 DEFAULT SYSDATETIME()
);
GO

-- ----------------------------------------------------------------------------
-- 6. BAŞLANGIÇ VERİLERİ (SEED DATA)
-- ----------------------------------------------------------------------------

-- Diller
INSERT INTO dbo.Diller (DilKodu, DilAdi) VALUES
('TR', 'Türkçe'),
('EN', 'English'),
('DE', 'Deutsch');

-- Dövizler
INSERT INTO dbo.DovizTurleri (DovizKodu, Sembol, DovizAdi) VALUES
('TRY', N'₺', 'Türk Lirası'),
('USD', '$', 'Amerikan Doları'),
('EUR', '€', 'Euro');

-- Kurlar (1 USD = 34.50 TRY, 1 EUR = 37.80 TRY)
INSERT INTO dbo.DovizKurlari (KaynakDoviz, HedefDoviz, AlisKuru, SatisKuru) VALUES
('USD', 'TRY', 34.2000, 34.5000),
('EUR', 'TRY', 37.4000, 37.8000),
('TRY', 'TRY', 1.0000, 1.0000);

-- Müşteri: Ahmet Yılmaz
INSERT INTO dbo.Musteriler (TcKimlikNo, Ad, Soyad, Telefon, TercihEdilenDil) VALUES
('12345678901', N'Ahmet', N'Yılmaz', '05551234567', 'TR');

-- Kart (Şifre: 1234 -> SHA256)
-- 1234'ün SHA-256 Hash'i: 03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4
INSERT INTO dbo.Kartlar (MusteriId, KartNumarasi, SifreHash, HataliGirisSayisi, BlokeMi, SonKullanimTarihi) VALUES
(1, '1234567890123456', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 0, 0, '12/28');

-- Hesaplar (Müşterinin TRY, USD ve EUR hesapları)
INSERT INTO dbo.Hesaplar (MusteriId, HesapNo, DovizKodu, Bakiye, AktifMi) VALUES
(1, 'TR01-1001-TRY', 'TRY', 50000.00, 1),
(1, 'TR01-1002-USD', 'USD', 2500.00, 1),  -- 2.500 USD bakiye
(1, 'TR01-1003-EUR', 'EUR', 1800.00, 1);  -- 1.800 EUR bakiye

-- ATM Tanımı
INSERT INTO dbo.Atmler (AtmKodu, Konum, AktifMi) VALUES
('ATM-001', N'Pazarlar MYO Kampüs Girişi', 1);

-- ATM Kasa Banknot Stoku (USD ve EUR kupürleri)
INSERT INTO dbo.AtmKasaStok (AtmId, DovizKodu, KupurDegeri, Adet) VALUES
-- USD Banknotları: (50x100$ = 5.000$), (40x50$ = 2.000$), (50x20$ = 1.000$)
(1, 'USD', 100, 50),
(1, 'USD', 50, 40),
(1, 'USD', 20, 50),
-- EUR Banknotları: (40x100€ = 4.000$), (30x50€ = 1.500$), (50x20€ = 1.000$)
(1, 'EUR', 100, 40),
(1, 'EUR', 50, 30),
(1, 'EUR', 20, 50);
GO

-- ----------------------------------------------------------------------------
-- 7. ATOMİK DÖVİZ ÇEKME STORED PROCEDURE (2-PHASE ACID TRANSACTION)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_DovizCek
    @AtmId INT,
    @KartId INT,
    @HesapId INT,
    @DovizKodu VARCHAR(3),
    @CekilecekTutar DECIMAL(18,2),
    @DonanimHatasiSimulasyonu BIT = 0, -- 1 verilirse donanım hatasını ve Rollback'i test eder
    @KalanBakiye DECIMAL(18,2) OUTPUT,
    @SonucKodu INT OUTPUT,             -- 0: Başarılı, 1: Yetersiz Bakiye, 2: Yetersiz ATM Stok, 3: Donanım Hatası / Rollback
    @Mesaj NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MevcutBakiye DECIMAL(18,2);
    DECLARE @ToplamAtmStok DECIMAL(18,2);
    DECLARE @GuncelKur DECIMAL(18,4);

    -- 1. Güncel Kuru Çek
    SELECT TOP 1 @GuncelKur = SatisKuru 
    FROM dbo.DovizKurlari 
    WHERE KaynakDoviz = @DovizKodu AND HedefDoviz = 'TRY';

    IF @GuncelKur IS NULL SET @GuncelKur = 1.0000;

    -- 2. Müşteri Bakiye Kontrolü
    SELECT @MevcutBakiye = Bakiye 
    FROM dbo.Hesaplar WITH (UPDLOCK, ROWLOCK)
    WHERE HesapId = @HesapId AND DovizKodu = @DovizKodu AND AktifMi = 1;

    IF @MevcutBakiye IS NULL OR @MevcutBakiye < @CekilecekTutar
    BEGIN
        SET @SonucKodu = 1;
        SET @Mesaj = N'Hesabınızda yeterli döviz bakiyesi bulunmamaktadır.';
        
        -- Başarısız işlem günlüğü
        INSERT INTO dbo.IslemGecmisi (AtmId, KartId, HesapId, IslemTipi, Tutar, DovizKodu, UygulananKur, IslemDurumu, HataMesaji)
        VALUES (@AtmId, @KartId, @HesapId, 'DOVIZ_CEKME', @CekilecekTutar, @DovizKodu, @GuncelKur, 'YETERSIZ_BAKIYE', @Mesaj);
        
        SET @KalanBakiye = ISNULL(@MevcutBakiye, 0);
        RETURN;
    END

    -- 3. ATM Kasa Fiziksel Nakit Kontrolü
    SELECT @ToplamAtmStok = SUM(KupurDegeri * Adet)
    FROM dbo.AtmKasaStok WITH (UPDLOCK, ROWLOCK)
    WHERE AtmId = @AtmId AND DovizKodu = @DovizKodu;

    IF @ToplamAtmStok IS NULL OR @ToplamAtmStok < @CekilecekTutar
    BEGIN
        SET @SonucKodu = 2;
        SET @Mesaj = N'ATM kasasında bu döviz türünde yeterli nakit bulunmamaktadır.';
        
        INSERT INTO dbo.IslemGecmisi (AtmId, KartId, HesapId, IslemTipi, Tutar, DovizKodu, UygulananKur, IslemDurumu, HataMesaji)
        VALUES (@AtmId, @KartId, @HesapId, 'DOVIZ_CEKME', @CekilecekTutar, @DovizKodu, @GuncelKur, 'YETERSIZ_KASA', @Mesaj);
        
        SET @KalanBakiye = @MevcutBakiye;
        RETURN;
    END

    -- 4. Atomik İşlem Başlatma (ACID Transaction)
    BEGIN TRANSACTION;
    BEGIN TRY
        -- A. Hesap Bakiyesinden Düşüm
        UPDATE dbo.Hesaplar
        SET Bakiye = Bakiye - @CekilecekTutar
        WHERE HesapId = @HesapId;

        -- B. Donanım Hatası / Para Dağıtıcı Sıkışması Test Kontrolü
        IF @DonanimHatasiSimulasyonu = 1
        BEGIN
            RAISERROR(N'Nakit dağıtıcı kapağında mekanik sıkışma tespit edildi (Hardware Failure)!', 16, 1);
        END

        -- C. İşlem Geçmişine Başarılı Kayıt
        INSERT INTO dbo.IslemGecmisi (AtmId, KartId, HesapId, IslemTipi, Tutar, DovizKodu, UygulananKur, IslemDurumu, HataMesaji)
        VALUES (@AtmId, @KartId, @HesapId, 'DOVIZ_CEKME', @CekilecekTutar, @DovizKodu, @GuncelKur, 'BASARILI', NULL);

        -- Tüm adımlar kusursuzsa işlemi onayla
        COMMIT TRANSACTION;

        SELECT @KalanBakiye = Bakiye FROM dbo.Hesaplar WHERE HesapId = @HesapId;
        SET @SonucKodu = 0;
        SET @Mesaj = N'Para çekme işlemi başarılı. Lütfen paranızı ve makbuzunuzu alınız.';
    END TRY
    BEGIN CATCH
        -- Hata durumunda işlemi geri al (ROLLBACK) -> Para asla kaybolmaz!
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @SonucKodu = 3;
        SET @Mesaj = ERROR_MESSAGE();

        -- Rollback durumunu denetim günlüğüne kaydet
        INSERT INTO dbo.IslemGecmisi (AtmId, KartId, HesapId, IslemTipi, Tutar, DovizKodu, UygulananKur, IslemDurumu, HataMesaji)
        VALUES (@AtmId, @KartId, @HesapId, 'DOVIZ_CEKME', @CekilecekTutar, @DovizKodu, @GuncelKur, 'DONANIM_HATASI_ROLLBACK', @Mesaj);

        SELECT @KalanBakiye = Bakiye FROM dbo.Hesaplar WHERE HesapId = @HesapId;
    END CATCH
END;
GO
