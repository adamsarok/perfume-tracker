import { showError } from "@/services/toasty-service";
import FileSelector from "./file-selector";
import { get } from "@/services/axios-service";

interface UploadComponentProps {
  onUpload: (guid: string | undefined) => void;
}

/*
4. Security Considerations
Validation: Ensure you validate the fileName to prevent path traversal vulnerabilities.
Size Limits: Implement checks to ensure files are within acceptable size limits to avoid Denial of Service (DoS) attacks.
Error Handling: Handle potential errors and edge cases, such as file upload failures or issues with the microservice communication.
*/

interface PresignedResponse {
  guid: string,
  presignedUrl: string,
}

export default function UploadComponent({ onUpload }: UploadComponentProps) {
  const handleUpload = async (file: File) => {
    try {
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

      const qry = `/images/get-presigned-url`;
      const presignedResponse = (await get<PresignedResponse>(qry)).data;

      const response = await fetch(presignedResponse.presignedUrl, {
        method: "PUT",
        body: fileBuffer,
        headers: {
          "Content-Type": file.type,
        },
      });
      if (!response.ok) {
        showError('Error uploading file:');
        return;
      }
      onUpload(presignedResponse.guid);
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
