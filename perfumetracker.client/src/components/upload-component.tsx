import { showError } from "@/services/toasty-service";
import FileSelector from "./file-selector";
import { put } from "@/services/axios-service";
interface UploadComponentProps {
  perfumeId: string | undefined;
}
interface UploadResponse {
  guid: string;
}

export default function UploadComponent({ perfumeId }: UploadComponentProps) {
  const handleUpload = async (file: File) : Promise<string> => {
    try {
      if (!perfumeId) {
        showError("Perfum is not saved");
        return "";
      }
      if (!file) {
        showError("File is empty");
        return "";
      }

      if (!file.name) {
        showError("Filename is required");
        return "";
      }

      const fileBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as ArrayBuffer);
        reader.onerror = reject;
        reader.readAsArrayBuffer(file);
      });

      const qry = `/images/upload/${encodeURIComponent(perfumeId)}`;
      const response = await put<UploadResponse>(qry, fileBuffer, {
        headers: {
          "Content-Type": file.type,
        },
      });
      if (!response.data.guid) {
        showError('Error uploading file:');
        return "";
      }
      return response.data.guid;
    } catch (error) {
      showError('Error uploading file:', error);
      return "";
    }
  };

  return (
    <div>
      <FileSelector handleUpload={handleUpload} />
    </div>
  );
}
