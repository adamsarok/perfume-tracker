import { useState } from "react";
import { Button } from "./ui/button";

export interface FileSelectorProps {
  handleUpload: (file: File) => Promise<string | undefined>;
}

export default function FileSelector({ handleUpload }: FileSelectorProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files.length > 0)
      setSelectedFile(event.target.files[0]);
  };

  return (
    <div>
      <input
        type="file"
        id="file-input"
        onChange={handleFileChange}
        style={{ display: "none" }}
      />
      <Button
        onClick={() => document.getElementById("file-input")?.click()}
        type="button"
      >
        {selectedFile ? selectedFile.name : "Choose File"}
      </Button>
      <Button
        type="button"
        className="ml-4"
        onClick={() => {
          if (selectedFile) handleUpload(selectedFile);
        }}
      >
        Upload File
      </Button>
    </div>
  );
}
