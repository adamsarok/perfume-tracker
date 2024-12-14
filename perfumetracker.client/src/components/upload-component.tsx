//import uploadFile from '@/services/upload-service';
import FileSelector from "./file-selector";
//import { R2_API_ADDRESS } from "@/services/conf";
import { toast } from "react-toastify";

interface UploadComponentProps {
  onUpload: (guid: string | undefined) => void;
}

/*
4. Security Considerations
Validation: Ensure you validate the fileName to prevent path traversal vulnerabilities.
Size Limits: Implement checks to ensure files are within acceptable size limits to avoid Denial of Service (DoS) attacks.
Error Handling: Handle potential errors and edge cases, such as file upload failures or issues with the microservice communication.
*/

const R2_API_ADDRESS = process.env.NEXT_PUBLIC_R2_API_ADDRESS;


export default function UploadComponent({ onUpload }: UploadComponentProps) {
  const handleUpload = async (file: File) => {
    console.log("handleUpload");
    console.log(R2_API_ADDRESS);
    try {
      if (!R2_API_ADDRESS) {
        toast.error("Error uploading file: R2 API address is not configured");
        return;
      }

      if (!file) {
        toast.error("File is empty");
        return;
      }

      if (!file.name) {
        toast.error("Filename is required");
        return;
      }

      const fileBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as ArrayBuffer);
        reader.onerror = reject;
        reader.readAsArrayBuffer(file);
      });

      const microserviceUrl = `${R2_API_ADDRESS}/upload-image?fileName=${encodeURIComponent(
        file.name
      )}`;
      const response = await fetch(microserviceUrl, {
        method: "PUT",
        body: fileBuffer,
        headers: {
          "Content-Type": "image/jpeg",
        },
      });
      const json = await response.json();
      console.log(json);
      if (!response.ok) {
        toast.error("error");
        return;
      }
      onUpload(json.guid);
    } catch (error) {
      toast.error("Error uploading file:" + error);
    }
  };

  return (
    <div>
      <FileSelector handleUpload={handleUpload} />
    </div>
  );
}
