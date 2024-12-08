/*
  Warnings:

  - You are about to drop the column `tagName` on the `PerfumeTag` table. All the data in the column will be lost.
  - The primary key for the `Tag` table will be changed. If it partially fails, the table could be left without primary key constraint.
  - Added the required column `tagId` to the `PerfumeTag` table without a default value. This is not possible if the table is not empty.

*/
-- DropForeignKey
ALTER TABLE "PerfumeTag" DROP CONSTRAINT "PerfumeTag_tagName_fkey";

-- AlterTable
ALTER TABLE "PerfumeTag" DROP COLUMN "tagName",
ADD COLUMN     "tagId" INTEGER NOT NULL;

-- AlterTable
ALTER TABLE "Tag" DROP CONSTRAINT "Tag_pkey",
ADD COLUMN     "id" SERIAL NOT NULL,
ADD CONSTRAINT "Tag_pkey" PRIMARY KEY ("id");

-- AddForeignKey
ALTER TABLE "PerfumeTag" ADD CONSTRAINT "PerfumeTag_tagId_fkey" FOREIGN KEY ("tagId") REFERENCES "Tag"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
