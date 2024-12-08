export const getImageUrl = (imageObjectKey: string | undefined, r2_api_address: string | undefined) => {
    if (!imageObjectKey || !r2_api_address) return "";
    return `${r2_api_address}/cached-image?key=${encodeURIComponent(imageObjectKey)}`;
}