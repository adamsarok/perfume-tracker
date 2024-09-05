
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import { Link } from "@nextui-org/react";
import * as perfumeRepo from "@/db/perfume-repo";
import WornList from "@/components/worn-list";
import router from "next/navigation";

export const dynamic = 'force-dynamic'

export default async function Home() {
  const perfumes = await perfumeRepo.getPerfumesForSelector();
  return (
    <div>
      <Link isBlock showAnchorIcon color="foreground" href="/perfumes/new-perfume">New Perfume</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/worn-list">Worn List</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/stats">Stats</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/calendar">Calendar</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/tags">Tags</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/recommendations">Recommendations</Link>
      <div className="mt-2">
      <PerfumeSelector perfumes={perfumes}/>
      </div>
      <div className="mt-2">
      <WornList />
      </div>
    </div>
  );
}
