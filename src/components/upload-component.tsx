import { Button } from '@nextui-org/button';
import { useState } from 'react';
import FileSelector from './file-selector';

interface UploadComponentProps {
  className: string,
  onUpload: (guid: string | undefined) => void
}

/*
4. Security Considerations
Validation: Ensure you validate the fileName to prevent path traversal vulnerabilities.
Size Limits: Implement checks to ensure files are within acceptable size limits to avoid Denial of Service (DoS) attacks.
Error Handling: Handle potential errors and edge cases, such as file upload failures or issues with the microservice communication.
*/

export default function UploadComponent({ className, onUpload }: UploadComponentProps) {
  const [uploadURL, setUploadURL] = useState('');

  // const uploadFile = async (file: any) => {
  //   const res = await fetch('/api/upload-image', {
  //     method: 'PUT',
  //   });

  //   const data = await res.json();
  //   setUploadURL(data.uploadURL); // Assuming the URL is returned as `url`
  //   console.log(uploadURL);
  // };

  const handleUpload = async (file: File) => {
    console.log(`uploading: ${file.name}`);
    const res = await fetch(`/api/upload-image?filename=${encodeURIComponent(file.name)}`, {
      method: 'PUT',
      body: file,
      headers: {
        'Content-Type': 'image/jpeg',
      },
    }); //what if not jpeg?
    //console.log(json);
    if (res.ok) {
      const json = await res.json();
      onUpload(json.guid);
    } else {
      console.error('Failed to upload file');
    }
  };

  return (
    <div>
      <FileSelector handleUpload={handleUpload} />
    </div>
  );
}