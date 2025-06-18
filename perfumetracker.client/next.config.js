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
            key: 'Content-Security-Policy',
            value: `
              default-src 'none';
              base-uri 'self';
              form-action 'self';
              frame-ancestors 'none';
              img-src 'self' https://*.r2.cloudflarestorage.com;
              script-src 'self' 'unsafe-inline' 'unsafe-eval';
              style-src 'self' 'unsafe-inline';
              font-src 'self' data:;
              connect-src 'self' https://perfume-tracker-235ug.ondigitalocean.app ws://perfume-tracker-235ug.ondigitalocean.app https://www.perfume-tracker.com ws://www.perfume-tracker.com https://perfume-tracker.com ws://perfume-tracker.com https://*.r2.cloudflarestorage.com http://localhost:* https://localhost:* ws://localhost:*;
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