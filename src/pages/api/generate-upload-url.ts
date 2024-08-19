// import type { NextApiRequest, NextApiResponse } from 'next'

// function generateGuid() {
//     return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
//         var r = Math.random()*16|0, v = c == 'x' ? r : (r&0x3|0x8);
//         return v.toString(16);
//     });
// }

// type ResponseData = {
//     guid: string,
//     url: string,
//     error: string
// }

// export default async function handler(req: NextApiRequest, res: NextApiResponse<ResponseData>) {
//   const guid = generateGuid();
//   const response = await fetch(`http://localhost:8080/generate-upload-url?key=${guid}`, {
//     method: 'GET',
//     headers: {
//       'Content-Type': 'application/json',
//     }
//   });

//   console.log(response);

//   if (response.ok) {
//     const data = await response.json();
//     console.log(data);
//     res.status(200).json({
//       guid,
//       url: data.uploadURL,
//       error: ''
//     });
//   } else {
//     res.status(response.status)
//       .json({ 
//         guid: '',
//         url: '',
//         error: 'Generate upload URL failed' });
//   }
// }