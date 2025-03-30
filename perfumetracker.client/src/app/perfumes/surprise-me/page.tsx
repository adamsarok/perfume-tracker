import { R2_API_ADDRESS } from "@/services/conf";
import SuprisePerfumeComponent from "./surprise-me-component";

export const dynamic = "force-dynamic";

export default function SuprisePerfumePage() {
  return (
    <SuprisePerfumeComponent r2_api_address={R2_API_ADDRESS} ></SuprisePerfumeComponent>
  );
}
