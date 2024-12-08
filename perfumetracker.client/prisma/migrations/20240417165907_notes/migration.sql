/*
  Warnings:

  - You are about to drop the column `ratingText` on the `Perfume` table. All the data in the column will be lost.

*/
-- AlterTable
ALTER TABLE "Perfume" DROP COLUMN "ratingText",
ADD COLUMN     "notes" TEXT NOT NULL DEFAULT '';
