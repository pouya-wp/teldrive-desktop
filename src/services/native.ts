import { invoke } from "@tauri-apps/api/core";
import { open } from "@tauri-apps/plugin-opener";
import type { LogLine, Status } from "../types";
export const native = {
 status: () => invoke<Status>("backend_status"),
 start: () => invoke<void>("start_backend"), restart: () => invoke<void>("restart_backend"),
 logs: () => invoke<LogLine[]>("backend_logs"), diagnostics: () => invoke<string>("diagnostics"),
 config: () => invoke<void>("open_config_folder"), select: () => invoke<void>("select_executable"),
 autostart: (enabled:boolean) => invoke<void>("set_autostart", { enabled }),
 openWeb: () => open("http://127.0.0.1:8080")
};
