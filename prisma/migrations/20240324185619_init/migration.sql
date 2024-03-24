-- CreateTable
CREATE TABLE "Perfume" (
    "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "house" TEXT NOT NULL,
    "perfume" TEXT NOT NULL,
    "rating" REAL NOT NULL
);

-- CreateTable
CREATE TABLE "PerfumeWorn" (
    "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "perfumeId" INTEGER NOT NULL,
    "wornOn" DATETIME NOT NULL,
    CONSTRAINT "PerfumeWorn_perfumeId_fkey" FOREIGN KEY ("perfumeId") REFERENCES "Perfume" ("id") ON DELETE RESTRICT ON UPDATE CASCADE
);
