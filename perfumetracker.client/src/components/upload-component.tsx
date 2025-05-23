import { showError } from "@/services/toasty-service";
import FileSelector from "./file-selector";

interface UploadComponentProps {
  onUpload: (guid: string | undefined) => void;
  r2_api_address: string | undefined;
}

/*
4. Security Considerations
Validation: Ensure you validate the fileName to prevent path traversal vulnerabilities.
Size Limits: Implement checks to ensure files are within acceptable size limits to avoid Denial of Service (DoS) attacks.
Error Handling: Handle potential errors and edge cases, such as file upload failures or issues with the microservice communication.
*/


export default function UploadComponent({ onUpload, r2_api_address }: UploadComponentProps) {
  const handleUpload = async (file: File) => {
    try {
      if (!r2_api_address) {
        showError("Error uploading file: R2 API address is not configured");
        return;
      }

      if (!file) {
        showError("File is empty");
        return;
      }

      if (!file.name) {
        showError("Filename is required");
        return;
      }

      const fileBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as ArrayBuffer);
        reader.onerror = reject;
        reader.readAsArrayBuffer(file);
      });

      const microserviceUrl = `${r2_api_address}/upload-image?fileName=${encodeURIComponent(
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
      if (!response.ok) {
        showError('Error uploading file:');
        return;
      }
      onUpload(json.guid);
    } catch (error) {
      showError('Error uploading file:', error);
    }
  };

  return (
    <div>
      <FileSelector handleUpload={handleUpload} />
    </div>
  );
}
