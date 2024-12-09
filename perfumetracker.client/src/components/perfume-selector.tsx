"use client";

import React from "react";
import SprayOnComponent from "./spray-on";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Separator } from "./ui/separator";
import { Button } from "./ui/button";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";

export interface PerfumeSelectorProps {
  perfumes: PerfumeWithWornStatsDTO[];
}

export default function PerfumeSelector({ perfumes }: PerfumeSelectorProps) {
  const onSprayOn = async () => {
    window.location.reload();
  };

  const perfumeMap = perfumes.map((x) => ({
    id: x.perfume.id,
    label: x.perfume.house + " - " + x.perfume.perfumeName,
  }));
  const [open, setOpen] = React.useState(false);
  const [value, setValue] = React.useState("");

  function getPerfumeID(): number {
    const res = perfumeMap.filter((x) => x.label === value);
    if (res && res.length > 0) return res[0].id;
    return 0;
  }

  return (
    <div>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className="flex w-full flex-wrap md:flex-nowrap gap-4"
          >
            {value
              ? perfumeMap.find((p) => p.label === value)?.label
              : "What are you wearing today?"}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[200px] p-0">
          <Command>
            <CommandInput placeholder="Search perfumes..." />
            <CommandList>
              <CommandEmpty>No perfume found.</CommandEmpty>
              <CommandGroup>
                {perfumeMap.map((p) => (
                  <CommandItem
                    key={p.label}
                    value={p.label}
                    onSelect={(currentValue) => {
                      setValue(currentValue === value ? "" : currentValue);
                      setOpen(false);
                    }}
                  >
                    {p.label}
                  </CommandItem>
                ))}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
      <SprayOnComponent
        perfumeId={getPerfumeID()}
        onSuccess={onSprayOn}
        className="mb-2 mt-2"
      ></SprayOnComponent>
      <Separator className="mb-1"></Separator>
    </div>
  );
}
