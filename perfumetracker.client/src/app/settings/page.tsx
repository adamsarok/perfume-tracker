"use client";

import { useSettingsStore } from "@/services/settings-service";
import { useEffect, useState } from "react";
import { Slider } from "@/components/ui/slider";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";

export const dynamic = "force-dynamic";

export default function SettingsPage() {
  const { settings, setSettings } = useSettingsStore();
  const [localSettings, setLocalSettings] = useState(settings);
  useEffect(() => {
    setLocalSettings(settings);
  }, [settings]);

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
          <span className="ml-4">{localSettings.minimumRating?.toFixed(1)}</span>
        </div>
      </div>
      <div>
        <Label htmlFor="dayFilter" className="block mb-2">
          Day filter - perfumes worn in the last X days will be excluded from recommendations
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
      <div className="mt-6 space-x-4">
        <Button onClick={() => setSettings(localSettings)}>Save</Button>
        <Button variant="secondary" onClick={() => setLocalSettings(settings)}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
