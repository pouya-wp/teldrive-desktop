use tauri::{menu::{Menu, MenuItem}, tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent}, AppHandle, Manager};
use crate::backend::manager::BackendManager;

pub fn setup(app: &AppHandle, manager: BackendManager) -> tauri::Result<()> {
 let open=MenuItem::with_id(app,"open","Open Teldrive",true,None::<&str>)?;
 let status=MenuItem::with_id(app,"status","Backend Status: Starting",false,None::<&str>)?;
 let restart=MenuItem::with_id(app,"restart","Restart Teldrive",true,None::<&str>)?;
 let browser=MenuItem::with_id(app,"browser","Open Web UI in Browser",true,None::<&str>)?;
 let config=MenuItem::with_id(app,"config","Open Config Folder",true,None::<&str>)?;
 let quit=MenuItem::with_id(app,"quit","Quit",true,None::<&str>)?;
 let menu=Menu::with_items(app,&[&open,&status,&restart,&browser,&config,&quit])?;
 let app_handle=app.clone(); let for_event=manager.clone();
 TrayIconBuilder::with_id("main-tray").menu(&menu).tooltip("Teldrive Desktop").icon(tauri::image::Image::new(&[32,32,32,255],1,1)).on_menu_event(move |app,event| match event.id.as_ref(){
  "open"=>{if let Some(w)=app.get_webview_window("main"){let _=w.show();let _=w.set_focus();}},
  "restart"=>{let m=for_event.clone();tauri::async_runtime::spawn(async move{let _=m.restart().await;});},
  "browser"=>{let _=tauri_plugin_opener::open_url("http://127.0.0.1:8080",None::<&str>);},
  "config"=>{let _=tauri_plugin_opener::open_path(crate::backend::discovery::config_path().parent().unwrap_or_default(),None::<&str>);},
  "quit"=>{for_event.stop();app.exit(0);}, _=>{}
 }).on_tray_icon_event(move |tray,event| { if let TrayIconEvent::Click{button:MouseButton::Left,button_state:MouseButtonState::Up,..}=event { if let Some(w)=tray.app_handle().get_webview_window("main"){let _=w.show();let _=w.set_focus();} } }).build(app)?;
 let _=app_handle; Ok(())
}
