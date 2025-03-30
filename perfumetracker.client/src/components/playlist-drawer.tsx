import { usePlaylistStore } from "@/services/playlist-service";
import { Button } from "./ui/button";
import {
  Drawer,
  DrawerTrigger,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
  DrawerDescription,
  DrawerFooter,
  DrawerClose,
} from "./ui/drawer";
import { Input } from "./ui/input";
import { PerfumeSelectTable } from "@/app/ai/perfume-select-table";
import { PerfumeSelectDto } from "@/app/ai/perfume-select-columns";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import React, { useCallback } from "react";

export interface PlaylistDrawerProps {
  readonly className: string;
  readonly perfume: PerfumeWithWornStatsDTO;
}

export default function PlaylistDrawer({ className, perfume }: PlaylistDrawerProps) {
  const playList = usePlaylistStore();
  const [forRemoval, setForRemoval] = React.useState<(PerfumeWithWornStatsDTO | undefined)[]>([]);
  
  const handleSelectionChange = useCallback(
    (selectedRows: PerfumeSelectDto[]) => {
      const perfumesToRemove = selectedRows.map((x) =>
        playList.perfumes.find((p) => p.perfume?.id === x.id)
      );
      setForRemoval(perfumesToRemove);
    },
    [playList.perfumes] // Dependencies
  );
  
  return (
    <div className={className}>
      <Drawer>
        <div className="flex items-center space-x-2">
          <DrawerTrigger asChild>
            {playList.perfumes.includes(perfume) ?
              <Button onClick={() => playList.setPerfumes(playList.perfumes.filter(p => p !== perfume))}
              >Remove from Playlist</Button> :
              <Button onClick={() => playList.setPerfumes([...playList.perfumes, perfume])}
              >Add to Playlist</Button>
            }
          </DrawerTrigger>
          <Input value={playList.playlistName} onChange={(event) => playList.setName(event.target.value)} placeholder="Playlist"></Input>
        </div>
        <DrawerContent>
          <DrawerHeader>
            <DrawerTitle>{playList.playlistName}</DrawerTitle>
          </DrawerHeader>
          <DrawerFooter>
            <PerfumeSelectTable perfumes={playList.perfumes.map(x => {
              return {
                id: x.perfume?.id,
                house: x.perfume?.house,
                perfume: x.perfume?.perfumeName,
                wornTimes: x.wornTimes,
              };
            })} onSelectionChange={handleSelectionChange}></PerfumeSelectTable>
            <Button onClick={() => playList.setPerfumes(
              playList.perfumes.filter((p) => !forRemoval.includes(p))
            )}   >Remove from Playlist</Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>
    </div>
  );
}
