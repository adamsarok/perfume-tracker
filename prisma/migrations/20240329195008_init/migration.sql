-- CreateTable
CREATE TABLE "Perfume" (
    "id" SERIAL NOT NULL,
    "house" TEXT NOT NULL,
    "perfume" TEXT NOT NULL,
    "rating" DOUBLE PRECISION NOT NULL,

    CONSTRAINT "Perfume_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "PerfumeWorn" (
    "id" SERIAL NOT NULL,
    "perfumeId" INTEGER NOT NULL,
    "wornOn" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "PerfumeWorn_pkey" PRIMARY KEY ("id")
);

-- AddForeignKey
ALTER TABLE "PerfumeWorn" ADD CONSTRAINT "PerfumeWorn_perfumeId_fkey" FOREIGN KEY ("perfumeId") REFERENCES "Perfume"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
