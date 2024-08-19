/*
  Warnings:

  - You are about to drop the column `imageUrl` on the `Perfume` table. All the data in the column will be lost.

*/
-- AlterTable
ALTER TABLE "Perfume" DROP COLUMN "imageUrl",
ADD COLUMN     "imageObjectKey" TEXT NOT NULL DEFAULT '';
