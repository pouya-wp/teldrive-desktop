use crate::{backend::{discovery, health}, config, logs::{self, LogLine}};
use serde::Serialize;
use std::{io::{BufRead, BufReader}, path::PathBuf, process::{Child, Command, Stdio}, sync::{Arc, Mutex}};

#[derive(Clone, Serialize, PartialEq, Debug)]
pub enum BackendState { Stopped, Starting, Running, Unhealthy, Restarting, Failed }
#[derive(Clone, Serialize)]
#[serde(rename_all="camelCase")]
pub struct Status { pub state: BackendState, pub detail: String, pub external: bool, pub version: Option<String>, pub executable: Option<String>, pub config_path: String, pub server_url: String }
struct Inner { status: Status, child: Option<Child>, logs: Vec<LogLine>, retries: u8 }
#[derive(Clone)]
pub struct BackendManager { inner: Arc<Mutex<Inner>> }

impl BackendManager {
 pub fn new() -> Self { let config=discovery::config_path(); Self { inner:Arc::new(Mutex::new(Inner { status:Status { state:BackendState::Stopped, detail:"Not started".into(), external:false, version:None, executable:None, config_path:config.display().to_string(), server_url:health::SERVER_URL.into() }, child:None,logs:vec![],retries:0 })) } }
 fn log(&self, stream:&str, text:impl AsRef<str>) { let mut i=self.inner.lock().unwrap(); i.logs.push(logs::line(stream,text)); if i.logs.len()>500 { i.logs.remove(0); } }
 pub fn status(&self)->Status { self.inner.lock().unwrap().status.clone() }
 pub fn logs(&self)->Vec<LogLine> { self.inner.lock().unwrap().logs.clone() }
 pub async fn start(&self) -> Result<(), String> {
   if health::healthy(health::SERVER_URL).await { let mut i=self.inner.lock().unwrap(); i.status.state=BackendState::Running;i.status.detail="Connected to Teldrive".into();i.status.external=i.child.is_none(); return Ok(()); }
   if std::net::TcpListener::bind("127.0.0.1:8080").is_err() { self.fail("Port 8080 is occupied by an application that is not responding as Teldrive."); return Err("port 8080 occupied".into()); }
   let cfg=discovery::config_path(); if !cfg.is_file() { self.fail("Teldrive configuration was not found."); return Err("configuration missing".into()); }
   let settings=config::load(); if settings.launch_backend==Some(false) { self.fail("Backend autostart is disabled in settings."); return Err("autostart disabled".into()); }
   let binary=discovery::find(settings.custom_executable.as_deref()).ok_or_else(|| { self.fail("Teldrive executable not found"); "teldrive executable not found".to_string() })?;
   let version=discovery::version(&binary).ok_or("Teldrive version verification failed")?;
   { let mut i=self.inner.lock().unwrap(); if i.child.is_some(){return Ok(())} i.status.state=BackendState::Starting;i.status.detail="Starting Teldrive…".into();i.status.external=false;i.status.executable=Some(binary.display().to_string());i.status.version=Some(version); }
   let mut cmd=Command::new(&binary); cmd.arg("run").stdin(Stdio::null()).stdout(Stdio::piped()).stderr(Stdio::piped());
   #[cfg(windows)] { use std::os::windows::process::CommandExt; cmd.creation_flags(0x08000000); }
   let mut child=cmd.spawn().map_err(|e| { self.fail(format!("Could not start Teldrive: {e}")); e.to_string() })?;
   if let Some(out)=child.stdout.take(){ self.capture(out,"stdout"); } if let Some(err)=child.stderr.take(){ self.capture(err,"stderr"); }
   self.inner.lock().unwrap().child=Some(child); self.log("system","Teldrive process launched");
   if health::wait_until_ready(health::SERVER_URL,30).await { let mut i=self.inner.lock().unwrap();i.status.state=BackendState::Running;i.status.detail="Teldrive is ready".into();i.retries=0; Ok(()) } else { self.fail("Backend failed to become ready. Check logs and port 8080."); Err("backend readiness timeout".into()) }
 }
 fn capture(&self, pipe:impl std::io::Read + Send + 'static, stream:&'static str) { let manager=self.clone(); std::thread::spawn(move || { for l in BufReader::new(pipe).lines().map_while(Result::ok) { manager.log(stream,l); } }); }
 fn fail(&self, detail:impl Into<String>) { let mut i=self.inner.lock().unwrap(); i.status.state=BackendState::Failed;i.status.detail=detail.into(); }
 pub async fn restart(&self)->Result<(),String>{ self.stop(); {let mut i=self.inner.lock().unwrap();i.status.state=BackendState::Restarting;i.status.detail="Restarting Teldrive…".into();} self.start().await }
 pub fn stop(&self) { let stopped={ let mut i=self.inner.lock().unwrap(); let stopped=if let Some(mut c)=i.child.take(){let _=c.kill();true}else{false}; i.status.state=BackendState::Stopped;i.status.detail="Stopped".into();stopped }; if stopped { self.log("system","Stopped app-managed Teldrive process"); } }
 pub async fn monitor(self) { loop { tokio::time::sleep(std::time::Duration::from_secs(2)).await; let exited={let mut i=self.inner.lock().unwrap(); i.child.as_mut().and_then(|c|c.try_wait().ok()).flatten().is_some()}; if exited { let retry={let mut i=self.inner.lock().unwrap();i.child=None;i.retries+=1;i.status.state=BackendState::Unhealthy;i.status.detail="Teldrive exited unexpectedly".into();i.retries}; if retry>4 {self.fail("Backend crashed repeatedly; automatic recovery stopped.");continue} let delay=[2,5,10,30][(retry-1)as usize]; self.log("system",format!("Backend crashed; retrying in {delay}s"));tokio::time::sleep(std::time::Duration::from_secs(delay)).await; let _=self.restart().await; } } }
}
