import { showError } from "@/services/toasty-service";
import FileSelector from "./file-selector";
import { put } from "@/services/axios-service";
interface UploadComponentProps {
  readonly perfumeId: string | undefined;
  readonly onUpload: (guid: string | undefined) => void;
}
interface UploadResponse {
  guid: string;  
}

export default function UploadComponent({ perfumeId, onUpload }: UploadComponentProps) {
  const handleUpload = async (file: File) => {
    try {
      if (!perfumeId) {
        showError("Perfume is not saved");
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

      const formData = new FormData();
      formData.append('file', file);

      const qry = `/images/upload/${encodeURIComponent(perfumeId)}`;
      const response = await put<UploadResponse>(qry, formData);
      if (response.error || !response.data?.guid) {
        showError('Error uploading file:', response.error ?? 'unknown error');
        return;
      }
      onUpload(response.data.guid);
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
