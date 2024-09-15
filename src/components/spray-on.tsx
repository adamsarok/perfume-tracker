import { MagicWand } from "@/icons/magic-wand";
import { Button, Input } from "@nextui-org/react";
import * as perfumeWornRepo from "../db/perfume-worn-repo";
import { toast } from "react-toastify";
import { useState } from "react";

export interface SprayOnProps {
    perfumeId: number | undefined
}

export default function SprayOnComponent({perfumeId} : SprayOnProps) {
    const yymmdd = new Date().toISOString().slice(0, 10);
    const [dateStr, setDateStr] = useState<string>(yymmdd);

    const onSprayOn = async () => {
        if (perfumeId) {
            var date: Date;
            if (dateStr < yymmdd) {
                date = new Date(dateStr);
                date.setHours(12, 0, 0);
            } else {
                date = new Date();
            }
            var result = await perfumeWornRepo.wearPerfume(perfumeId, date);
            if (result.ok) toast.success("Smell on!");
            else toast.error(result.error);
        }
    }

    return (
        <div>
        <Button color="secondary"
            startContent={<MagicWand />}
            onPress={() => onSprayOn()}
            className="ml-2 mr-4"
                >Spray On</Button>
        <Input type="date" 
            defaultValue={yymmdd}
            onValueChange={setDateStr}
        >
            Worn On
            </Input>
        </div>
    )
}