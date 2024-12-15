'use client';

import { getDashboardUrl } from "@/services/metabase-service";
import React, { useEffect, useState } from "react";

export const dynamic = 'force-dynamic'

export default function StatsPage() {
    const [iframeUrl, setIframeUrl] = useState<string | null>(null);

    useEffect(() => {
      const fetchIframeUrl = async () => {
        try {
          const url = await getDashboardUrl();
          if (url) setIframeUrl(url);
        } catch (error) {
          console.error('Error fetching dashboard URL:', error);
        }
      };
  
      fetchIframeUrl();
    }, []);
  
    if (!iframeUrl) {
      return <div>Loading...</div>;
    }
  
    return (
      <div style={{ width: '100%', height: '100vh' }}>
        <iframe
          src={iframeUrl}
          frameBorder="0"
          width="100%"
          height="100%"
          allowFullScreen
        ></iframe>
      </div>
    );
}