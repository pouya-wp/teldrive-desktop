use chrono::Utc;
use serde::Serialize;

#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct LogLine { pub at: String, pub stream: String, pub text: String }
pub fn redact(input: &str) -> String {
    // Do not persist complete connection URLs or common secret assignments.
    input.split_whitespace().map(|word| if word.contains("://") && word.contains('@') || word.to_lowercase().contains("password=") || word.to_lowercase().contains("secret=") { "[REDACTED]" } else { word }).collect::<Vec<_>>().join(" ")
}
pub fn line(stream: &str, text: impl AsRef<str>) -> LogLine { LogLine { at: Utc::now().to_rfc3339(), stream: stream.into(), text: redact(text.as_ref()) } }
