-- SQLite schema for monAppGestion (code-first)
PRAGMA foreign_keys = OFF;

CREATE TABLE IF NOT EXISTS "Products" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Code" TEXT NOT NULL,
    "Nom" TEXT NOT NULL,
    "Qte" INTEGER NOT NULL,
    "PrixAchat" TEXT NOT NULL,
    "PrixVente" TEXT NOT NULL,
    "DateExpiration" TEXT
);

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "Password" TEXT NOT NULL,
    "Rank" TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Ventes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NumVente TEXT,
    Date TEXT,
    Versement REAL DEFAULT 0,
    IdClient INTEGER
);

CREATE TABLE IF NOT EXISTS VenteDetails (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IdVente INTEGER,
    IdProduit INTEGER,
    PrixVente REAL,
    Qte INTEGER
);

CREATE TABLE IF NOT EXISTS Clients (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nom TEXT NOT NULL,
    Adresse TEXT,
    Telephone TEXT
);

CREATE TABLE IF NOT EXISTS Fournisseurs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nom TEXT NOT NULL,
    Adresse TEXT,
    Telephone TEXT
);

CREATE TABLE IF NOT EXISTS Achats (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NumAchat TEXT,
    Date TEXT,
    IdFournisseur INTEGER,
    Versement REAL DEFAULT 0,
    FOREIGN KEY(IdFournisseur) REFERENCES Fournisseurs(Id)
);

CREATE TABLE IF NOT EXISTS AchatDetails (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IdAchat INTEGER,
    IdProduit INTEGER,
    PrixAchat REAL,
    Qte INTEGER
);
