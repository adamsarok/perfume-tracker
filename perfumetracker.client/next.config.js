/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  async headers() {
    return [
      {
        // Apply to all routes
        source: '/(.*)',
        headers: [
          {
            key: 'Content-Security-Policy-Report-Only',
            value: `
              default-src 'none';
              base-uri 'self';
              form-action 'self';
              frame-ancestors 'none';
              img-src 'self' https://*.r2.cloudflarestorage.com;
              script-src 'self' 'unsafe-inline' 'unsafe-eval';
              style-src 'self' 'unsafe-inline';
              font-src 'self' data:;
              connect-src 'self' https://perfume-tracker-235ug.ondigitalocean.app https://www.perfume-tracker.com https://perfume-tracker.com http://localhost:* https://localhost:* https://*.r2.cloudflarestorage.com;
              object-src 'none';
              media-src 'none';
              worker-src 'none';
              manifest-src 'self';
              upgrade-insecure-requests;
              block-all-mixed-content;
            `.replace(/\s{2,}/g, ' ').trim() // Minimize whitespace
          }
        ]
      }
    ]
  }
}

module.exports = nextConfig;