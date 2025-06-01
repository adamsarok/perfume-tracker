import LogsComponent from "./log-component";

export default function LogsPage() {
  const apiUrl = process.env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS;
  if (!apiUrl) {
    console.error("API URL not configured");
    return;
  }

  return <div>
    <h1>Logs</h1>
    <LogsComponent apiUrl={apiUrl}></LogsComponent>
  </div>
} 