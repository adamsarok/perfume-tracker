import { useState } from "react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { wearPerfume } from "@/services/perfume-worn-service";
import { showError, showSuccess } from "@/services/toasty-service";
import { WandSparkles } from "lucide-react";
import { useAuth } from "@/hooks/use-auth";

export interface SprayOnProps {
  readonly perfumeId: string | undefined;
  readonly onSuccess: (() => void) | null;
  readonly className: string;
  readonly isRandomPerfume: boolean;
}

export default function SprayOnComponent({
  perfumeId,
  onSuccess,
  className,
  isRandomPerfume,
}: SprayOnProps) {
  const yymmdd = new Date().toISOString().slice(0, 10);
  const [dateStr, setDateStr] = useState<string>(yymmdd);

  const handleDateChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setDateStr(event.target.value);
  };

  const auth = useAuth();

  const onSprayOn = async () => {
    if (auth.guardAction()) return;
    if (perfumeId) {
      let date: Date;
      if (dateStr < yymmdd) {
        date = new Date(dateStr);
        date.setHours(12, 0, 0);
      } else {
        date = new Date();
      }
      const result = await wearPerfume(perfumeId, date, isRandomPerfume);
      if (result.ok) {
        showSuccess("Smell on!");
        if (onSuccess) onSuccess();
      } else showError(result.error);
    }
  };

  return (
    <div className={"flex items-center space-x-4 " + className}>
      <Button type="button" color="secondary" onClick={() => onSprayOn()} className="flex-2">
        <WandSparkles /> Spray On
      </Button>
      <Label htmlFor="date">Worn On</Label>
      <Input
        type="date"
        className="flex-1"
        id="date"
        value={dateStr}
        onChange={handleDateChange}
      />
    </div>
  );
}
