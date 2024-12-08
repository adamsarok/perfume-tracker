/*
  Warnings:

  - You are about to drop the `PerfumeTag` table. If the table is not empty, all the data it contains will be lost.
  - You are about to drop the `Tag` table. If the table is not empty, all the data it contains will be lost.

*/
-- DropForeignKey
ALTER TABLE "PerfumeTag" DROP CONSTRAINT "PerfumeTag_perfumeId_fkey";

-- DropForeignKey
ALTER TABLE "PerfumeTag" DROP CONSTRAINT "PerfumeTag_tagId_fkey";

-- DropTable
DROP TABLE "PerfumeTag";

-- DropTable
DROP TABLE "Tag";
