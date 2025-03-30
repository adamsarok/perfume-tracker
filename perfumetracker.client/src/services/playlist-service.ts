import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { create } from "zustand";
import { persist } from "zustand/middleware";

// export interface PerfumeForPlaylist {
//   id: number;
//   house: string;
//   perfumeName: string;
// }

interface PlaylistStore {
  playlistName: string;
  perfumes: PerfumeWithWornStatsDTO[];
  setName: (newName: string) => void;
  setPerfumes: (newPerfumes: PerfumeWithWornStatsDTO[]) => void;
}

//TODO: users should build playlists of perfumes, which would work like a shopping cart functions:
//1. add perfumes to playlist from perfume finder or edit form
//2. manage playlists form: edit playlist, select current playlist
//3. can set playlist to filter finder, recommendations (eg create Winter playlist for fragrances to wear in winter)
export const usePlaylistStore = create<PlaylistStore>()(
  persist(
    (set) => ({
      playlistName: "Default",
      perfumes: [],
      setName: (newName) => {
        set(() => ({ playlistName: newName }));
      },
      setPerfumes: (newPerfumes) => {
        set(() => ({
          perfumes: newPerfumes,
        }));
      },
    }), //TODO: persist in DB
    { name: "perfume-tracker-playlist" }
  )
);
