use tauri_plugin_autostart::ManagerExt;
pub fn set<R: tauri::Runtime>(app:&tauri::AppHandle<R>, enabled:bool)->Result<(),String>{ if enabled { app.autolaunch().enable().map_err(|e|e.to_string()) } else { app.autolaunch().disable().map_err(|e|e.to_string()) } }
