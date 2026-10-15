-- ============================================================================
-- PROJE: ATM DÖVİZ ÇEKME SİSTEMİ VERİTABANI TASARIMI (SQLITE UYUMLU)
-- ============================================================================

PRAGMA foreign_keys = ON;

-- Diller Tablosu
CREATE TABLE IF NOT EXISTS Diller (
    DilKodu TEXT PRIMARY KEY,
    DilAdi TEXT NOT NULL
);

-- Döviz Türleri
CREATE TABLE IF NOT EXISTS DovizTurleri (
    DovizKodu TEXT PRIMARY KEY,
    Sembol TEXT NOT NULL,
    DovizAdi TEXT NOT NULL
);

-- Anlık Kurlar
CREATE TABLE IF NOT EXISTS DovizKurlari (
    KurId INTEGER PRIMARY KEY AUTOINCREMENT,
    KaynakDoviz TEXT NOT NULL,
    HedefDoviz TEXT NOT NULL,
    AlisKuru REAL NOT NULL,
    SatisKuru REAL NOT NULL,
    GuncellenmeTarihi TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (KaynakDoviz) REFERENCES DovizTurleri(DovizKodu),
    FOREIGN KEY (HedefDoviz) REFERENCES DovizTurleri(DovizKodu)
);

-- Müşteriler
CREATE TABLE IF NOT EXISTS Musteriler (
    MusteriId INTEGER PRIMARY KEY AUTOINCREMENT,
    TcKimlikNo TEXT UNIQUE NOT NULL,
    Ad TEXT NOT NULL,
    Soyad TEXT NOT NULL,
    Telefon TEXT NOT NULL,
    TercihEdilenDil TEXT DEFAULT 'TR',
    OlusturulmaTarihi TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (TercihEdilenDil) REFERENCES Diller(DilKodu)
);

-- Kartlar (PIN SHA-256)
CREATE TABLE IF NOT EXISTS Kartlar (
    KartId INTEGER PRIMARY KEY AUTOINCREMENT,
    MusteriId INTEGER NOT NULL,
    KartNumarasi TEXT UNIQUE NOT NULL,
    SifreHash TEXT NOT NULL,
    HataliGirisSayisi INTEGER DEFAULT 0,
    BlokeMi INTEGER DEFAULT 0,
    SonKullanimTarihi TEXT NOT NULL,
    FOREIGN KEY (MusteriId) REFERENCES Musteriler(MusteriId)
);

-- Müşteri Hesapları
CREATE TABLE IF NOT EXISTS Hesaplar (
    HesapId INTEGER PRIMARY KEY AUTOINCREMENT,
    MusteriId INTEGER NOT NULL,
    HesapNo TEXT UNIQUE NOT NULL,
    DovizKodu TEXT NOT NULL,
    Bakiye REAL NOT NULL DEFAULT 0.0 CHECK (Bakiye >= 0),
    AktifMi INTEGER DEFAULT 1,
    FOREIGN KEY (MusteriId) REFERENCES Musteriler(MusteriId),
    FOREIGN KEY (DovizKodu) REFERENCES DovizTurleri(DovizKodu)
);

-- ATM Cihazları
CREATE TABLE IF NOT EXISTS Atmler (
    AtmId INTEGER PRIMARY KEY AUTOINCREMENT,
    AtmKodu TEXT UNIQUE NOT NULL,
    Konum TEXT NOT NULL,
    AktifMi INTEGER DEFAULT 1
);

-- ATM Banknot Kasa Stoku
CREATE TABLE IF NOT EXISTS AtmKasaStok (
    StokId INTEGER PRIMARY KEY AUTOINCREMENT,
    AtmId INTEGER NOT NULL,
    DovizKodu TEXT NOT NULL,
    KupurDegeri INTEGER NOT NULL,
    Adet INTEGER NOT NULL CHECK (Adet >= 0),
    UNIQUE (AtmId, DovizKodu, KupurDegeri),
    FOREIGN KEY (AtmId) REFERENCES Atmler(AtmId),
    FOREIGN KEY (DovizKodu) REFERENCES DovizTurleri(DovizKodu)
);

-- İşlem Geçmişi (Audit Log)
CREATE TABLE IF NOT EXISTS IslemGecmisi (
    IslemId INTEGER PRIMARY KEY AUTOINCREMENT,
    AtmId INTEGER NOT NULL,
    KartId INTEGER NOT NULL,
    HesapId INTEGER NOT NULL,
    IslemTipi TEXT NOT NULL,
    Tutar REAL NOT NULL,
    DovizKodu TEXT NOT NULL,
    UygulananKur REAL NOT NULL,
    IslemDurumu TEXT NOT NULL,
    HataMesaji TEXT,
    IslemTarihi TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AtmId) REFERENCES Atmler(AtmId),
    FOREIGN KEY (KartId) REFERENCES Kartlar(KartId),
    FOREIGN KEY (HesapId) REFERENCES Hesaplar(HesapId),
    FOREIGN KEY (DovizKodu) REFERENCES DovizTurleri(DovizKodu)
);

-- Başlangıç Verileri (Seed Data)
INSERT OR IGNORE INTO Diller VALUES ('TR', 'Türkçe'), ('EN', 'English'), ('DE', 'Deutsch');
INSERT OR IGNORE INTO DovizTurleri VALUES ('TRY', '₺', 'Türk Lirası'), ('USD', '$', 'Amerikan Doları'), ('EUR', '€', 'Euro');
INSERT OR IGNORE INTO DovizKurlari (KaynakDoviz, HedefDoviz, AlisKuru, SatisKuru) VALUES 
('USD', 'TRY', 34.20, 34.50),
('EUR', 'TRY', 37.40, 37.80);

INSERT OR IGNORE INTO Musteriler (MusteriId, TcKimlikNo, Ad, Soyad, Telefon, TercihEdilenDil)
VALUES (1, '12345678901', 'Ahmet', 'Yılmaz', '05551234567', 'TR');

-- Şifre 1234 -> SHA256: 03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4
INSERT OR IGNORE INTO Kartlar (KartId, MusteriId, KartNumarasi, SifreHash, HataliGirisSayisi, BlokeMi, SonKullanimTarihi)
VALUES (1, 1, '1234567890123456', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 0, 0, '12/28');

INSERT OR IGNORE INTO Hesaplar (HesapId, MusteriId, HesapNo, DovizKodu, Bakiye) VALUES
(1, 1, 'TR01-1001-TRY', 'TRY', 50000.00),
(2, 1, 'TR01-1002-USD', 'USD', 2500.00),
(3, 1, 'TR01-1003-EUR', 'EUR', 1800.00);

INSERT OR IGNORE INTO Atmler (AtmId, AtmKodu, Konum) VALUES (1, 'ATM-001', 'Pazarlar MYO Kampüs');

INSERT OR IGNORE INTO AtmKasaStok (AtmId, DovizKodu, KupurDegeri, Adet) VALUES
(1, 'USD', 100, 50),
(1, 'USD', 50, 40),
(1, 'USD', 20, 50),
(1, 'EUR', 100, 40),
(1, 'EUR', 50, 30),
(1, 'EUR', 20, 50);
