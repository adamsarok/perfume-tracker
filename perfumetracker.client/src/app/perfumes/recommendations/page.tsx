"use client";

import React from "react";
import RecommendationsList from "./recommendations-list";

export const dynamic = 'force-dynamic'

export default function RecommendationsPage() {
  return (
    <div>
      <div className="mt-2">
        <RecommendationsList />
      </div>
    </div>
  );
}
