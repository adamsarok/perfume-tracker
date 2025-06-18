"use client";

import { useEffect, useState } from "react";
import { Slider } from "@/components/ui/slider";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Separator } from "@/components/ui/separator";
import {getUserProfile, updateUserProfile, UserProfile } from "@/services/user-profiles-service";
import { showError, showSuccess } from "@/services/toasty-service";
import Link from "next/link";
import { CircleX, FileText, SaveIcon } from "lucide-react";

export const dynamic = "force-dynamic";

export default function SettingsPage() {
  const [localSettings, setLocalSettings] = useState<UserProfile | null>(null);
  const [settings, setSettings] = useState<UserProfile | null>(null);

  useEffect(() => {
    async function fetchSettings() {
      const fetchedSettings = await getUserProfile();
      setSettings(fetchedSettings);
      setLocalSettings(fetchedSettings);
    }
    fetchSettings();
  }, []);

  if (!localSettings) {
    return <div>Loading...</div>;
  }

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
        <Label htmlFor="sprayAmountFullSizeMl" className="block mb-2">
          Ml used per wear - Full Size Bottles
        </Label>
        <div className="flex items-center space-x-2 mb-2">
          <Slider
            id="sprayAmountFullSizeMl"
            value={[localSettings.sprayAmountFullSizeMl]}
            max={1}
            step={0.1}
            min={0}
            onValueChange={(value) =>
              setLocalSettings({ ...localSettings, sprayAmountFullSizeMl: value[0] })
            }
          />
          <span className="ml-4">{localSettings.sprayAmountFullSizeMl?.toFixed(1)}</span>
        </div>
      </div>
      <Separator className="my-4" />
      <div>
        <Label htmlFor="sprayAmountSamplesMl" className="block mb-2">
          Ml used per wear - Samples
        </Label>
        <div className="flex items-center space-x-2 mb-2">
          <Slider
            id="sprayAmountSamplesMl"
            value={[localSettings.sprayAmountSamplesMl]}
            max={1}
            step={0.1}
            min={0}
            onValueChange={(value) =>
              setLocalSettings({ ...localSettings, sprayAmountSamplesMl: value[0] })
            }
          />
          <span className="ml-4">{localSettings.sprayAmountSamplesMl?.toFixed(1)}</span>
        </div>
      </div>
      <Separator className="my-4" />
      <div className="mt-6 space-x-4">
        <Button
          onClick={async () => {
            const result = await updateUserProfile(localSettings);
            if (result.error) showError("Settings set failed", result.error);
            else showSuccess("Settings set");
          }}
        >
         <SaveIcon /> Save
        </Button>
        <Button variant="secondary" onClick={() => setLocalSettings(settings)}>
          <CircleX /> Cancel
        </Button>
      </div>
    </div>
  );
}
