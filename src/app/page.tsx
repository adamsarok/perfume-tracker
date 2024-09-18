
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import { Link } from "@nextui-org/react";
import * as perfumeRepo from "@/db/perfume-repo";
import WornList from "@/components/worn-list";
import router from "next/navigation";
import { R2_API_ADDRESS } from "@/services/conf";

export const dynamic = 'force-dynamic'

export default async function Home() {
  const perfumes = await perfumeRepo.getPerfumesForSelector();
  return (
    <div>
      <div className="mt-2">
      <PerfumeSelector perfumes={perfumes}/>
      </div>
      <div className="mt-2">
      <WornList r2_api_address={R2_API_ADDRESS} />
      </div>
    </div>
  );
}
