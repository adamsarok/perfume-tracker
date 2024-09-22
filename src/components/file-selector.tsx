import { Button } from '@nextui-org/react';
import { useState } from 'react';

export interface FileSelectorProps {
    handleUpload: (file: File) => Promise<void>
}

export default function FileSelector({handleUpload}: FileSelectorProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files.length > 0) setSelectedFile(event.target.files[0]);
  };

  return (
    <div>
      <input
        type="file"
        id="file-input"
        onChange={handleFileChange}
        style={{ display: 'none' }}
      />
      <Button as="label" htmlFor="file-input">
        {selectedFile ? selectedFile.name : 'Choose File'}
      </Button>
      <Button 
        className='ml-4' 
        onClick={() => {
            if (selectedFile) handleUpload(selectedFile);
        }}>Upload File</Button>
    </div>
  );
}