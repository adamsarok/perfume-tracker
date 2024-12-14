//"use server";

// import { R2_API_ADDRESS } from "@/services/conf";
// import { ActionResultGUID } from "@/dto/ActionResultGUID";


// export default async function uploadFile(file: File): Promise<ActionResultGUID> {
//   try {
//     if (!R2_API_ADDRESS)
//       return {
//         ok: false,
//         error: `Error uploading file: R2 API address is not configured`,
//       };

//     if (!file) return { ok: false, error: "File is empty" };

//     if (!file.name) return { ok: false, error: "Filename is required" };

//     const fileBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
//       const reader = new FileReader();
//       reader.onload = () => resolve(reader.result as ArrayBuffer);
//       reader.onerror = reject;
//       reader.readAsArrayBuffer(file);
//     });

//     const microserviceUrl = `${R2_API_ADDRESS}/upload-image?fileName=${encodeURIComponent(
//       file.name
//     )}`;
//     const response = await fetch(microserviceUrl, {
//       method: "PUT",
//       body: fileBuffer,
//       headers: {
//         "Content-Type": "image/jpeg",
//       },
//     });
//     const json = await response.json();
//     console.log(json);
//     if (!response.ok) {
//       return {
//         ok: false,
//         error: `Failed to upload file to microservice: ${response.statusText}`,
//       };
//     }
//     return { ok: true, guid: json.guid };
//   } catch (error) {
//     console.error("Error uploading file:", error);
//     return {
//       ok: false,
//       error: `Error uploading file: ${(error as Error)?.message}`,
//     };
//   }
// }
