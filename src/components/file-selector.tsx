import { Button, Input } from '@nextui-org/react';
import { useState } from 'react';

export interface FileSelectorProps {
    handleUpload: (file: File) => Promise<void>
}

export default function FileSelector({handleUpload}: FileSelectorProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const handleFileChange = (event: any) => {
    setSelectedFile(event.target.files[0]);
  };

//   const handleUpload = async () => {
//     if (!selectedFile) {
//       alert('Please select a file first.');
//       return;
//     }

//     const formData = new FormData();
//     formData.append('file', selectedFile);

//     try {
//       const response = await fetch('/api/upload', {
//         method: 'POST',
//         body: formData,
//       });

//       const result = await response.json();
//       console.log(result);
//     } catch (error) {
//       console.error('Error uploading file:', error);
//     }
//   };

  return (
    <div>
      {/* <Input type="file" onChange={handleFileChange} /> */}
      {/* Hidden native file input */}
      <input
        type="file"
        id="file-input"
        onChange={handleFileChange}
        style={{ display: 'none' }}
      />
      {/* Styled button as label */}
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