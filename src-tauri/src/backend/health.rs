use std::time::Duration;

pub const SERVER_URL: &str = "http://127.0.0.1:8080";

pub async fn healthy(url: &str) -> bool {
    let Ok(client) = reqwest::Client::builder().timeout(Duration::from_secs(2)).build() else { return false; };
    match client.get(url).send().await { Ok(response) => response.status().is_success() || response.status().is_redirection(), Err(_) => false }
}

pub async fn wait_until_ready(url: &str, seconds: u64) -> bool {
    for _ in 0..seconds * 2 { if healthy(url).await { return true; } tokio::time::sleep(Duration::from_millis(500)).await; }
    false
}
