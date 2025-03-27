"use client";

import { useEffect, useState } from "react";
import { Slider } from "@/components/ui/slider";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Separator } from "@/components/ui/separator";
import { getSettings, PerfumeSettings, updateSettings } from "@/services/settings-service";
import { showError, showSuccess } from "@/services/toasty-service";

export const dynamic = "force-dynamic";

// export interface SettingsPageProps {
//   readonly settings: PerfumeSettings;
// }

export default function SettingsPage() {
  const [localSettings, setLocalSettings] = useState<PerfumeSettings | null>(null);
  const [settings, setSettings] = useState<PerfumeSettings | null>(null);

  useEffect(() => {
    async function fetchSettings() {
      const fetchedSettings = await getSettings(); // Fetch settings
      setSettings(fetchedSettings); // Update `settings`
      setLocalSettings(fetchedSettings); // Update `localSettings` with the fetched settings
    }
    fetchSettings();
  }, []);

  if (!localSettings) {
    return <div>Loading...</div>;
  }

  // const [localSettings, setLocalSettings] = useState({});
  // useEffect(() => {
  //   setLocalSettings(settings);
  // }, [settings]);

  return (
    <div>
      <div>
        <Label htmlFor="minimumRating" className="block mb-2">
          Minimum rating for Perfumes - suggestions & filtering
        </Label>
        <div className="flex items-center space-x-2 mb-2">
          <Slider
            id="minimumRating"
            value={[localSettings.minimumRating]}
            max={10}
            step={0.5}
            min={0}
            onValueChange={(value) =>
              setLocalSettings({ ...localSettings, minimumRating: value[0] })
            }
          />
          <span className="ml-4">
            {localSettings.minimumRating?.toFixed(1)}
          </span>
        </div>
      </div>
      <Separator className="my-4" />
      <div>
        <Label htmlFor="dayFilter" className="block mb-2">
          Day filter - perfumes worn in the last X days will be excluded from
          recommendations
        </Label>
        <div className="flex items-center space-x-2 mb-2">
          <Slider
            id="dayFilter"
            value={[localSettings.dayFilter]}
            max={365}
            step={1}
            min={0}
            onValueChange={(value) =>
              setLocalSettings({ ...localSettings, dayFilter: value[0] })
            }
          />
          <span className="ml-4">{localSettings.dayFilter?.toFixed(0)}</span>
        </div>
      </div>
      <Separator className="my-4" />
      <div>
        <Label htmlFor="dayFilter" className="block mb-2">
          Gender filter - only perfumes marketed for selected genders will be
          recommended
        </Label>
        <div className="flex items-center justify-stretch space-x-2 mb-2">
          <div className="flex items-center space-x-2">
            <Checkbox
              id="female"
              defaultChecked={localSettings.showFemalePerfumes}
              onCheckedChange={(checked) =>
                (localSettings.showFemalePerfumes = checked as boolean)
              }
            >
              Female
            </Checkbox>
            <label
              htmlFor="female"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              Female
            </label>
          </div>
          <div className="flex items-center space-x-2">
            <Checkbox
              id="unisex"
              defaultChecked={localSettings.showUnisexPerfumes}
              onCheckedChange={(checked) =>
                (localSettings.showUnisexPerfumes = checked as boolean)
              }
            >
              Unisex
            </Checkbox>
            <label
              htmlFor="unisex"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              Unisex
            </label>
          </div>
          <div className="flex items-center space-x-2">
            <Checkbox
              id="male"
              defaultChecked={localSettings.showMalePerfumes}
              onCheckedChange={(checked) =>
                (localSettings.showMalePerfumes = checked as boolean)
              }
            >
              Male
            </Checkbox>
            <label
              htmlFor="male"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              Male
            </label>
          </div>
        </div>
      </div>
      <Separator className="my-4" />
      <div>
        <Label htmlFor="sprayAmount" className="block mb-2">
          Ml used per wear - (1 spray = 0.1ml, default 2 sprays)
        </Label>
        <div className="flex items-center space-x-2 mb-2">
          <Slider
            id="sprayAmount"
            value={[localSettings.sprayAmount]}
            max={1}
            step={0.1}
            min={0}
            onValueChange={(value) =>
              setLocalSettings({ ...localSettings, sprayAmount: value[0] })
            }
          />
          <span className="ml-4">{localSettings.sprayAmount?.toFixed(1)}</span>
        </div>
      </div>
      <Separator className="my-4" />
      <div className="mt-6 space-x-4">
        <Button
          onClick={async () => {
            const result = await updateSettings(localSettings);
            if (result.error) showError("Settings set failed", result.error);
            else showSuccess("Settings set");
          }}
        >
          Save
        </Button>
        <Button variant="secondary" onClick={() => setLocalSettings(settings)}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
