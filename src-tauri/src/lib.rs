mod backend; mod config; mod logs; mod autostart; mod tray;
use backend::manager::BackendManager;
use tauri::{Manager, Emitter};

#[tauri::command] fn backend_status(manager:tauri::State<'_,BackendManager>)->backend::manager::Status{manager.status()}
#[tauri::command] async fn start_backend(manager:tauri::State<'_,BackendManager>)->Result<(),String>{manager.start().await}
#[tauri::command] async fn restart_backend(manager:tauri::State<'_,BackendManager>)->Result<(),String>{manager.restart().await}
#[tauri::command] fn backend_logs(manager:tauri::State<'_,BackendManager>)->Vec<logs::LogLine>{manager.logs()}
#[tauri::command] fn diagnostics(manager:tauri::State<'_,BackendManager>)->String{let s=manager.status();format!("Teldrive Desktop\nState: {:?}\nServer: {}\nConfig: {}\nExecutable: {}\nVersion: {}",s.state,s.server_url,s.config_path,s.executable.unwrap_or_else(||"not found".into()),s.version.unwrap_or_else(||"unknown".into()))}
#[tauri::command] fn open_config_folder()->Result<(),String>{let path=backend::discovery::config_path();tauri_plugin_opener::open_path(path.parent().ok_or("config folder unavailable")?,None::<&str>).map_err(|e|e.to_string())}
#[tauri::command] fn select_executable(app:tauri::AppHandle)->Result<(),String>{use tauri_plugin_dialog::DialogExt;let handle=app.clone();app.dialog().file().add_filter("Teldrive executable",&["exe"]).pick_file(move|file|{if let Some(file)=file{let mut s=config::load();s.custom_executable=Some(file.as_path().unwrap().display().to_string());let _=config::save(&s);let _=handle.emit("teldrive-path-changed",());}});Ok(())}
#[tauri::command] fn set_autostart(app:tauri::AppHandle,enabled:bool)->Result<(),String>{autostart::set(&app,enabled)}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run(){
 let manager=BackendManager::new(); let monitor=manager.clone(); let autostart=std::env::args().any(|a|a=="--autostart");
 tauri::Builder::default().plugin(tauri_plugin_autostart::init(tauri_plugin_autostart::MacosLauncher::LaunchAgent,Some(vec!["--autostart"]))).plugin(tauri_plugin_opener::init()).plugin(tauri_plugin_dialog::init()).manage(manager.clone()).setup(move|app|{tray::setup(app.handle(),manager.clone())?;if autostart{if let Some(w)=app.get_webview_window("main"){let _=w.hide();}}let m=manager.clone();tauri::async_runtime::spawn(async move{let _=m.start().await;});tauri::async_runtime::spawn(monitor.monitor());Ok(())}).on_window_event(|window,event|{if let tauri::WindowEvent::CloseRequested{api,..}=event{api.prevent_close();let _=window.hide();}}).invoke_handler(tauri::generate_handler![backend_status,start_backend,restart_backend,backend_logs,diagnostics,open_config_folder,select_executable,set_autostart]).run(tauri::generate_context!()).expect("error running Teldrive Desktop");
}
