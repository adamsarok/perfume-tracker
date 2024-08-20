
import React, { useCallback, useEffect, useState } from "react";
import PerfumeSelector from "../components/perfume-selector";
import PerfumeCard from "@/components/perfumecard";
import { Link } from "@nextui-org/react";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import * as perfumeRepo from "@/db/perfume-repo";
import { Perfume } from "@prisma/client";
import WornList from "@/components/worn-list";

export const dynamic = 'force-dynamic'

export default async function Home() {
  const perfumes = await perfumeRepo.getPerfumesForSelector();
  
  return (
    <div >
      <Link isBlock showAnchorIcon color="foreground" href="/perfumes/new-perfume">New Perfume</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/worn-list">Worn List</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/stats">Stats</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/tags">Tags</Link>
      <PerfumeSelector perfumes={perfumes} />
      <WornList />
    </div>
  );
}
