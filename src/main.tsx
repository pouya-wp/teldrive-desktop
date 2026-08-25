import { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { native } from "./services/native";
import type { Status } from "./types";
import "./styles.css";

const initial: Status = { state:"Starting", detail:"Finding Teldrive", external:false, configPath:"", serverUrl:"http://127.0.0.1:8080" };
function App() {
 const [status,setStatus]=useState(initial); const [settings,setSettings]=useState(false); const [logs,setLogs]=useState<string[]>([]);
 useEffect(()=>{ const tick=async()=>{ try { setStatus(await native.status()); } catch { /* backend is initializing */ } }; void tick(); const id=setInterval(()=>void tick(),700); return()=>clearInterval(id); },[]);
 useEffect(()=>{ if(status.state==="Starting"||status.state==="Restarting") void native.start().catch(()=>undefined); },[]);
 if(settings) return <section className="panel"><button className="back" onClick={()=>setSettings(false)}>← Back</button><h1>Settings</h1><label><input type="checkbox" defaultChecked onChange={e=>void native.autostart(e.target.checked)}/> Start with Windows</label><p>Autostart runs minimized and starts Teldrive in the background.</p><dl><dt>Backend</dt><dd>{status.executable||"Not detected"}</dd><dt>Config</dt><dd>{status.configPath}</dd><dt>Server</dt><dd>{status.serverUrl}</dd><dt>Version</dt><dd>{status.version||"—"}</dd></dl><div className="actions"><button onClick={()=>void native.restart()}>Restart backend</button><button onClick={()=>void native.config()}>Open config folder</button><button onClick={()=>void native.select()}>Select executable</button></div></section>;
 if(status.state==="Running") return <main className="web"><header><span>Teldrive Desktop</span><small>{status.external?"Connected to external backend":"Backend running"}</small><button onClick={()=>setSettings(true)}>Settings</button></header><iframe title="Teldrive" src={status.serverUrl}/></main>;
 const failed=status.state==="Failed"||status.state==="Unhealthy";
 return <section className="splash"><div className="mark">T</div><h1>Teldrive Desktop</h1><div className={failed?"indicator error":"indicator"}/><h2>{status.detail}</h2><p>{failed?"Teldrive could not become ready. Your configuration and existing data have not been changed.":"Starting the existing Teldrive service securely in the background."}</p><div className="actions"><button onClick={()=>void native.restart()}>Retry</button>{failed&&<button onClick={()=>void native.select()}>Select Teldrive executable</button>}<button onClick={()=>void native.config()}>Open config folder</button><button onClick={()=>void native.logs().then(x=>setLogs(x.map(l=>`[${l.stream}] ${l.text}`)))}>View logs</button></div>{logs.length>0&&<pre>{logs.join("\n")}</pre>}</section>;
}
createRoot(document.getElementById("root")!).render(<App/>);
