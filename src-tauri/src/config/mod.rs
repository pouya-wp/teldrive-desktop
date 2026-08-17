use serde::{Deserialize, Serialize};
use std::{fs, path::PathBuf};

#[derive(Default, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Settings { pub custom_executable: Option<String>, pub server_url: Option<String>, pub launch_backend: Option<bool>, pub close_to_tray: Option<bool> }
pub fn path() -> PathBuf { dirs::config_dir().unwrap_or_else(std::env::temp_dir).join("Teldrive Desktop").join("settings.json") }
pub fn load() -> Settings { fs::read_to_string(path()).ok().and_then(|s| serde_json::from_str(&s).ok()).unwrap_or_default() }
pub fn save(settings: &Settings) -> Result<(), String> { let p=path(); fs::create_dir_all(p.parent().ok_or("invalid settings path")?).map_err(|e|e.to_string())?; fs::write(p, serde_json::to_vec_pretty(settings).map_err(|e|e.to_string())?).map_err(|e|e.to_string()) }
