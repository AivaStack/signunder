//! The SignUnder Identity CA (ADR 0014). Both keys stay in Cloud KMS.
//!
//! ```text
//! identity-ca root  --ca-key KEY [--common-name CN] [--organization O] [--out FILE]
//! identity-ca issue --ca-key KEY --ca-cert FILE --subject-key KEY
//!                   --organization NAME --domain DOMAIN [--days N] [--out FILE]
//! ```
//!
//! `root` makes the self-signed root certificate. `issue` makes a company's identity
//! certificate for the public half of `--subject-key` (the key that signs identity
//! assertions), naming the company and its verified domain, and writes the chain:
//! the company's certificate, then the root. KEYs are Cloud KMS key versions;
//! credentials come from Application Default Credentials.

use std::process::ExitCode;
use std::sync::Arc;

use rcgen::Issuer;
use sign_sidecar::identity_ca;
use sign_sidecar::kms::Kms;
use sign_sidecar::kms_key::KmsKey;

struct Args {
    command: String,
    flags: std::collections::HashMap<String, String>,
}

fn parse_args() -> Result<Args, String> {
    let mut args = std::env::args().skip(1);
    let command = args.next().ok_or("give a command: root or issue")?;
    let mut flags = std::collections::HashMap::new();
    while let Some(flag) = args.next() {
        let value = args.next().ok_or_else(|| format!("{flag} needs a value"))?;
        flags.insert(flag.trim_start_matches("--").to_owned(), value);
    }
    Ok(Args { command, flags })
}

impl Args {
    fn need(&self, name: &str) -> Result<String, String> {
        self.flags
            .get(name)
            .cloned()
            .ok_or_else(|| format!("--{name} is required"))
    }
}

async fn run(args: Args) -> Result<String, String> {
    let kms = Arc::new(Kms::from_env().await);
    let ca_key = KmsKey::load(kms.clone(), args.need("ca-key")?).await?;
    match args.command.as_str() {
        "root" => {
            let params = identity_ca::root_params(
                args.flags
                    .get("common-name")
                    .map_or("SignUnder Identity CA", String::as_str),
                args.flags
                    .get("organization")
                    .map_or("SignUnder", String::as_str),
            )?;
            tokio::task::spawn_blocking(move || {
                params
                    .self_signed(&ca_key)
                    .map(|cert| cert.pem())
                    .map_err(|e| format!("signing the root: {e}"))
            })
            .await
            .map_err(|e| e.to_string())?
        }
        "issue" => {
            let root_pem = std::fs::read_to_string(args.need("ca-cert")?)
                .map_err(|e| format!("reading --ca-cert: {e}"))?;
            let subject = KmsKey::load(kms, args.need("subject-key")?).await?;
            let days = args
                .flags
                .get("days")
                .map_or(Ok(365), |d| d.parse::<i64>())
                .map_err(|_| "--days must be a number")?;
            let params =
                identity_ca::leaf_params(&args.need("organization")?, &args.need("domain")?, days)?;
            tokio::task::spawn_blocking(move || {
                let issuer = Issuer::from_ca_cert_pem(&root_pem, ca_key)
                    .map_err(|e| format!("reading the root: {e}"))?;
                params
                    .signed_by(&subject, &issuer)
                    .map(|cert| format!("{}{}", cert.pem(), root_pem.trim_end().to_owned() + "\n"))
                    .map_err(|e| format!("signing the certificate: {e}"))
            })
            .await
            .map_err(|e| e.to_string())?
        }
        other => Err(format!("unknown command {other}: use root or issue")),
    }
}

#[tokio::main]
async fn main() -> ExitCode {
    sign_sidecar::internal_tls::install_crypto_provider();
    let args = match parse_args() {
        Ok(args) => args,
        Err(e) => {
            eprintln!("identity-ca: {e}");
            return ExitCode::from(2);
        }
    };
    let out = args.flags.get("out").cloned();
    match run(args).await {
        Ok(pem) => match out {
            Some(path) => match std::fs::write(&path, pem) {
                Ok(()) => ExitCode::SUCCESS,
                Err(e) => {
                    eprintln!("identity-ca: writing {path}: {e}");
                    ExitCode::FAILURE
                }
            },
            None => {
                print!("{pem}");
                ExitCode::SUCCESS
            }
        },
        Err(e) => {
            eprintln!("identity-ca: {e}");
            ExitCode::FAILURE
        }
    }
}
