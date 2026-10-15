//! Google credentials for a Fly.io machine (ADR 0011, ADR 0012).
//!
//! Workload identity federation exchanges a Fly.io identity token for Google access. The
//! credential configuration names the token file Fly.io writes, `/.fly/oidc_token`, but a
//! suspended machine wakes with that file, and the auth library's cached access token, as
//! old as the suspension: both are timed by clocks that stop while the machine is
//! suspended. So on Fly.io the sidecar asks Fly.io's machine API for a fresh token at
//! every exchange, and reads the file only if that fails.

use std::sync::Arc;
use std::time::Duration;

use google_cloud_auth::credentials::Credentials;
use google_cloud_auth::credentials::external_account::ProgrammaticBuilder;
use google_cloud_auth::credentials::subject_token::{
    Builder as SubjectTokenBuilder, SubjectToken, SubjectTokenProvider,
};
use google_cloud_auth::errors::SubjectTokenProviderError;
use serde_json::Value;
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::UnixStream;

/// Fly.io's machine API, reachable from inside every machine.
pub const FLY_API_SOCKET: &str = "/.fly/api";

/// Credentials for the configuration in `GOOGLE_APPLICATION_CREDENTIALS`, if it is a
/// workload identity federation configuration reading a Fly.io token file. `None` means
/// "not on Fly.io": use Application Default Credentials as they are.
pub fn credentials_from_env() -> Option<Result<Credentials, String>> {
    let path = std::env::var("GOOGLE_APPLICATION_CREDENTIALS").ok()?;
    let config: Value = serde_json::from_str(&std::fs::read_to_string(&path).ok()?).ok()?;
    let identity = FlyIdentity::for_config(
        &config,
        FLY_API_SOCKET,
        &std::env::var("FLY_OIDC_AUDIENCE").unwrap_or_else(|_| "sts.amazonaws.com".into()),
    )?;
    Some(build(&config, identity))
}

fn build(config: &Value, identity: FlyIdentity) -> Result<Credentials, String> {
    let field = |name: &str| {
        config[name]
            .as_str()
            .ok_or_else(|| format!("{name} missing from the credential configuration"))
    };
    let mut builder = ProgrammaticBuilder::new(Arc::new(identity))
        .with_audience(field("audience")?)
        .with_subject_token_type(field("subject_token_type")?)
        .with_token_url(field("token_url")?);
    if let Some(url) = config["service_account_impersonation_url"].as_str() {
        builder = builder.with_target_principal(impersonated_account(url)?);
    }
    builder.build().map_err(|e| e.to_string())
}

/// The service account in `…/serviceAccounts/{email}:generateAccessToken`.
fn impersonated_account(url: &str) -> Result<&str, String> {
    url.rsplit_once("/serviceAccounts/")
        .and_then(|(_, rest)| rest.strip_suffix(":generateAccessToken"))
        .ok_or_else(|| format!("unexpected service_account_impersonation_url {url}"))
}

/// Fetches a Fly.io identity token for each exchange.
#[derive(Debug)]
pub struct FlyIdentity {
    socket: String,
    audience: String,
    /// The token file Fly.io writes, read if the machine API does not answer.
    file: String,
}

impl FlyIdentity {
    /// For an external-account configuration whose token file is Fly.io's.
    fn for_config(config: &Value, socket: &str, audience: &str) -> Option<Self> {
        let file = config["credential_source"]["file"].as_str()?;
        (config["type"] == "external_account" && file.starts_with("/.fly/")).then(|| Self {
            socket: socket.into(),
            audience: audience.into(),
            file: file.into(),
        })
    }

    async fn ask_machine_api(&self) -> Result<String, String> {
        let request = async {
            let mut stream = UnixStream::connect(&self.socket)
                .await
                .map_err(|e| e.to_string())?;
            let body = serde_json::json!({ "aud": self.audience }).to_string();
            // HTTP/1.0, so the answer is not chunked and ends when the connection closes.
            let head = format!(
                "POST /v1/tokens/oidc HTTP/1.0\r\nHost: localhost\r\nContent-Type: application/json\r\nContent-Length: {}\r\n\r\n",
                body.len()
            );
            stream
                .write_all([head.as_bytes(), body.as_bytes()].concat().as_slice())
                .await
                .map_err(|e| e.to_string())?;
            let mut answer = Vec::new();
            stream
                .read_to_end(&mut answer)
                .await
                .map_err(|e| e.to_string())?;
            token_from_response(&String::from_utf8_lossy(&answer))
        };
        tokio::time::timeout(Duration::from_secs(5), request)
            .await
            .map_err(|_| "no answer within 5 s".to_string())?
    }
}

/// The JWT in a machine API answer: the body, possibly as a JSON string.
fn token_from_response(answer: &str) -> Result<String, String> {
    let (head, body) = answer
        .split_once("\r\n\r\n")
        .ok_or("malformed HTTP answer")?;
    let status = head.lines().next().unwrap_or_default();
    if status.split_whitespace().nth(1) != Some("200") {
        return Err(format!("answered {status}"));
    }
    let token = body.trim().trim_matches('"');
    if token.split('.').count() != 3 {
        return Err("the answer is not a JWT".into());
    }
    Ok(token.to_owned())
}

#[derive(Debug)]
pub struct FlyIdentityError(String);

impl std::fmt::Display for FlyIdentityError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "Fly.io identity token: {}", self.0)
    }
}

impl std::error::Error for FlyIdentityError {}

impl SubjectTokenProviderError for FlyIdentityError {
    fn is_transient(&self) -> bool {
        true
    }
}

impl FlyIdentity {
    /// A fresh token from the machine API, or the token file's if it does not answer.
    pub async fn token(&self) -> Result<String, FlyIdentityError> {
        Ok(match self.ask_machine_api().await {
            Ok(token) => token,
            Err(api) => match tokio::fs::read_to_string(&self.file).await {
                Ok(token) if !token.trim().is_empty() => {
                    println!(
                        "Fly.io machine API gave no token ({api}); using {}",
                        self.file
                    );
                    token.trim().to_owned()
                }
                _ => {
                    return Err(FlyIdentityError(format!(
                        "machine API: {api}; {} unreadable",
                        self.file
                    )));
                }
            },
        })
    }
}

impl SubjectTokenProvider for FlyIdentity {
    type Error = FlyIdentityError;

    async fn subject_token(&self) -> Result<SubjectToken, Self::Error> {
        Ok(SubjectTokenBuilder::new(self.token().await?).build())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    const JWT: &str = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJ4In0.c2lnbmF0dXJl";

    fn config(file: &str) -> Value {
        serde_json::json!({
            "type": "external_account",
            "audience": "//iam.googleapis.com/projects/1/locations/global/workloadIdentityPools/p/providers/q",
            "subject_token_type": "urn:ietf:params:oauth:token-type:jwt",
            "token_url": "https://sts.googleapis.com/v1/token",
            "credential_source": { "file": file },
            "service_account_impersonation_url": "https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/signer@p.iam.gserviceaccount.com:generateAccessToken",
        })
    }

    #[test]
    fn only_fly_token_files_switch_to_the_machine_api() {
        assert!(FlyIdentity::for_config(&config("/.fly/oidc_token"), "s", "a").is_some());
        assert!(FlyIdentity::for_config(&config("/var/run/token"), "s", "a").is_none());
        let mut key = config("/.fly/oidc_token");
        key["type"] = "service_account".into();
        assert!(FlyIdentity::for_config(&key, "s", "a").is_none());
    }

    #[tokio::test]
    async fn builds_credentials_impersonating_the_configured_account() {
        let config = config("/.fly/oidc_token");
        assert_eq!(
            impersonated_account(
                config["service_account_impersonation_url"]
                    .as_str()
                    .unwrap()
            ),
            Ok("signer@p.iam.gserviceaccount.com")
        );
        let identity = FlyIdentity::for_config(&config, "s", "a").unwrap();
        assert!(build(&config, identity).is_ok());
    }

    #[test]
    fn reads_the_token_from_plain_or_quoted_answers_and_refuses_errors() {
        let ok = |body: &str| format!("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n{body}");
        assert_eq!(token_from_response(&ok(JWT)).as_deref(), Ok(JWT));
        assert_eq!(
            token_from_response(&ok(&format!("\"{JWT}\"\n"))).as_deref(),
            Ok(JWT)
        );
        assert!(token_from_response("HTTP/1.0 403 Forbidden\r\n\r\nno").is_err());
        assert!(token_from_response(&ok("not a token")).is_err());
    }

    /// A stand-in for the machine API on a Unix socket: the token comes from it, and from
    /// the file once it stops answering.
    #[tokio::test]
    async fn asks_the_machine_api_then_falls_back_to_the_file() {
        let dir = std::env::temp_dir().join(format!("fly-identity-{}", std::process::id()));
        std::fs::create_dir_all(&dir).unwrap();
        let socket = dir.join("api");
        let file = dir.join("oidc_token");
        std::fs::write(&file, "file.token.value\n").unwrap();
        let _ = std::fs::remove_file(&socket);
        let listener = tokio::net::UnixListener::bind(&socket).unwrap();
        let server = tokio::spawn(async move {
            let (mut stream, _) = listener.accept().await.unwrap();
            let mut request = vec![0; 4096];
            let n = stream.read(&mut request).await.unwrap();
            let request = String::from_utf8_lossy(&request[..n]).into_owned();
            stream
                .write_all(format!("HTTP/1.0 200 OK\r\n\r\n{JWT}").as_bytes())
                .await
                .unwrap();
            request
        });
        let identity = FlyIdentity {
            socket: socket.to_string_lossy().into_owned(),
            audience: "sts.amazonaws.com".into(),
            file: file.to_string_lossy().into_owned(),
        };

        assert_eq!(identity.token().await.unwrap(), JWT);
        let request = server.await.unwrap();
        assert!(request.starts_with("POST /v1/tokens/oidc HTTP/1.0"));
        assert!(request.ends_with(r#"{"aud":"sts.amazonaws.com"}"#));

        std::fs::remove_file(&socket).unwrap();
        assert_eq!(identity.token().await.unwrap(), "file.token.value");
        std::fs::remove_dir_all(&dir).unwrap();
    }
}
