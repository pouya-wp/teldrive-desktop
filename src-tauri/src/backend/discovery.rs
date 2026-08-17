use std::{env, path::{Path, PathBuf}, process::Command};

pub fn config_path() -> PathBuf { dirs::home_dir().unwrap_or_default().join(".teldrive").join("config.toml") }

pub fn find(saved: Option<&str>) -> Option<PathBuf> {
    let mut candidates = vec![dirs::home_dir()?.join(".installer").join("bin").join("teldrive.exe")];
    if let Some(path) = saved { candidates.push(PathBuf::from(path)); }
    if let Ok(output) = Command::new("where").arg("teldrive").output() {
        candidates.extend(String::from_utf8_lossy(&output.stdout).lines().map(PathBuf::from));
    }
    if let Ok(program_files) = env::var("ProgramFiles") { candidates.push(PathBuf::from(program_files).join("Teldrive").join("teldrive.exe")); }
    candidates.into_iter().filter(|p| p.is_file()).filter(|p| version(p).is_some()).max_by_key(|p| version(p).unwrap_or_default())
}

pub fn version(binary: &Path) -> Option<String> {
    let output = Command::new(binary).arg("version").output().ok()?;
    if output.status.success() { Some(String::from_utf8_lossy(&output.stdout).trim().to_owned()) } else { None }
}
