export type BackendState = "Stopped" | "Starting" | "Running" | "Unhealthy" | "Restarting" | "Failed";
export interface Status { state: BackendState; detail: string; external: boolean; version?: string; executable?: string; configPath: string; serverUrl: string; }
export interface LogLine { at: string; stream: "stdout" | "stderr" | "system"; text: string; }
