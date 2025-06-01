import { env } from "process";
import SuprisePerfumeComponent from "./surprise-me-component";

export const dynamic = "force-dynamic";

export default function SuprisePerfumePage() {
  return (
    <SuprisePerfumeComponent r2_api_address={env.NEXT_PUBLIC_R2_API_ADDRESS} ></SuprisePerfumeComponent>
  );
}
