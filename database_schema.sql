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
    Date TEXT
);

CREATE TABLE IF NOT EXISTS VenteDetails (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IdVente INTEGER,
    IdProduit INTEGER,
    PrixVente REAL,
    Qte INTEGER
);
