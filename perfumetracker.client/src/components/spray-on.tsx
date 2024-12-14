import { MagicWand } from "@/icons/magic-wand";
import { toast } from "react-toastify";
import { useState } from "react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { wearPerfume } from "@/services/perfume-worn-service";

export interface SprayOnProps {
  perfumeId: number | undefined;
  onSuccess: (() => void) | null;
  className: string;
}

export default function SprayOnComponent({
  perfumeId,
  onSuccess,
  className,
}: SprayOnProps) {
  const yymmdd = new Date().toISOString().slice(0, 10);
  const [dateStr, setDateStr] = useState<string>(yymmdd);

  const handleDateChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setDateStr(event.target.value);
  };

  const onSprayOn = async () => {
    if (perfumeId) {
      let date: Date;
      if (dateStr < yymmdd) {
        date = new Date(dateStr);
        date.setHours(12, 0, 0);
      } else {
        date = new Date();
      }
      const result = await wearPerfume(perfumeId, date);
      if (result.ok) {
        toast.success("Smell on!");
        if (onSuccess) onSuccess();
      } else toast.error(result.error);
    }
  };

  return (
    <div className={"flex items-center space-x-4 " + className}>
      <Button color="secondary" onClick={() => onSprayOn()} className="flex-2">
        <MagicWand /> Spray On
      </Button>
      <Label htmlFor="date">Worn On</Label>
      <Input
        type="date"
        className="flex-1"
        id="date"
        value={dateStr}
        //defaultValue={yymmdd}
        onChange={handleDateChange}
      />
    </div>
  );
}
