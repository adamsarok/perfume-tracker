-- DropForeignKey
ALTER TABLE "PerfumeTag" DROP CONSTRAINT "PerfumeTag_perfumeId_fkey";

-- DropForeignKey
ALTER TABLE "PerfumeWorn" DROP CONSTRAINT "PerfumeWorn_perfumeId_fkey";

-- AddForeignKey
ALTER TABLE "PerfumeWorn" ADD CONSTRAINT "PerfumeWorn_perfumeId_fkey" FOREIGN KEY ("perfumeId") REFERENCES "Perfume"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "PerfumeTag" ADD CONSTRAINT "PerfumeTag_perfumeId_fkey" FOREIGN KEY ("perfumeId") REFERENCES "Perfume"("id") ON DELETE CASCADE ON UPDATE CASCADE;
