import { Button } from '@nextui-org/button';
import { useState } from 'react';

interface UploadComponentProps {
  className: string
}

export default function UploadComponent({ className }: UploadComponentProps) {
  const [uploadURL, setUploadURL] = useState('');

  const generateUploadURL = async () => {
    const res = await fetch('/api/generate-upload-url', {
      method: 'POST',
    });

    const data = await res.json();
    setUploadURL(data.uploadURL); // Assuming the URL is returned as `url`
    console.log(uploadURL);
  };

  const handleUpload = async (file: any) => {
    if (!uploadURL) return;

    const res = await fetch(uploadURL, {
      method: 'PUT',
      body: file,
    });

    if (res.ok) {
      console.log('File uploaded successfully');
    } else {
      console.error('Failed to upload file');
    }
  };

  return (
    <div>
      <Button color='success' className={className} onClick={generateUploadURL}>Upload image</Button>
      {/* File input or drag-and-drop area here */}
    </div>
  );
}