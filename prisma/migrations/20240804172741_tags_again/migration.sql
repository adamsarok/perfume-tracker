-- CreateTable
CREATE TABLE "PerfumeTag" (
    "id" SERIAL NOT NULL,
    "perfumeId" INTEGER NOT NULL,
    "tagName" TEXT NOT NULL,

    CONSTRAINT "PerfumeTag_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "Tag" (
    "tag" TEXT NOT NULL,
    "color" TEXT NOT NULL,

    CONSTRAINT "Tag_pkey" PRIMARY KEY ("tag")
);

-- AddForeignKey
ALTER TABLE "PerfumeTag" ADD CONSTRAINT "PerfumeTag_perfumeId_fkey" FOREIGN KEY ("perfumeId") REFERENCES "Perfume"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "PerfumeTag" ADD CONSTRAINT "PerfumeTag_tagName_fkey" FOREIGN KEY ("tagName") REFERENCES "Tag"("tag") ON DELETE RESTRICT ON UPDATE CASCADE;
