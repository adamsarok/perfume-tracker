import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface PerfumeSettings {
  minimumRating: number;
  dayFilter: number;
  showMalePerfumes: boolean;
  showUnisexPerfumes: boolean;
  showFemalePerfumes: boolean;
}

interface SettingsStore {
  settings: PerfumeSettings;
  setSettings: (newSettings: Partial<PerfumeSettings>) => void;
}

export const useSettingsStore = create<SettingsStore>()(
  persist(
    (set) => ({
      settings: {
        minimumRating: 8,
        dayFilter: 30,
        showMalePerfumes: true,
        showUnisexPerfumes: true,
        showFemalePerfumes: true,
      },
      setSettings: (newSettings) => {
        set((state) => ({
          settings: { ...state.settings, ...newSettings },
        }));
      },
    }), //TODO: persist in DB
    { name: "perfume-tracker-user-settings" }
  )
);