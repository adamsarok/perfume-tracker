-- CreateTable
CREATE TABLE "PerfumeSuggested" (
    "id" SERIAL NOT NULL,
    "perfumeId" INTEGER NOT NULL,
    "suggestedOn" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "PerfumeSuggested_pkey" PRIMARY KEY ("id")
);

-- AddForeignKey
ALTER TABLE "PerfumeSuggested" ADD CONSTRAINT "PerfumeSuggested_perfumeId_fkey" FOREIGN KEY ("perfumeId") REFERENCES "Perfume"("id") ON DELETE CASCADE ON UPDATE CASCADE;
