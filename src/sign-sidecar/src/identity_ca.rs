//! The SignUnder Identity CA's certificates (ADR 0014): a root, and for each company
//! whose domain is verified and whose name Eoin approved, a certificate naming it that
//! signs its CAWG identity assertions. The leaf settings pass c2pa-rs's certificate
//! profile check (see identity.rs's tests), so verifiers report the assertion as
//! well-formed; whether they trust the CA is theirs to decide.

use rcgen::{
    BasicConstraints, CertificateParams, DnType, ExtendedKeyUsagePurpose, IsCa, KeyUsagePurpose,
    SanType, SerialNumber,
};
use sha2::{Digest, Sha256};
use time::{Duration, OffsetDateTime};

/// id-kp-documentSigning (RFC 9336).
const DOCUMENT_SIGNING: &[u64] = &[1, 3, 6, 1, 5, 5, 7, 3, 36];

fn serial(seed: &str) -> SerialNumber {
    let now = OffsetDateTime::now_utc().unix_timestamp_nanos();
    let digest = Sha256::digest(format!("{seed}|{now}|{}", std::process::id()));
    // 16 bytes, positive.
    let mut bytes = digest[..16].to_vec();
    bytes[0] &= 0x7f;
    SerialNumber::from_slice(&bytes)
}

/// The root: a CA for identity certificates only, valid for ten years.
pub fn root_params(common_name: &str, organization: &str) -> Result<CertificateParams, String> {
    let mut params = CertificateParams::new(Vec::<String>::new()).map_err(|e| e.to_string())?;
    params.distinguished_name = rcgen::DistinguishedName::new();
    params
        .distinguished_name
        .push(DnType::OrganizationName, organization);
    params
        .distinguished_name
        .push(DnType::CommonName, common_name);
    params.is_ca = IsCa::Ca(BasicConstraints::Constrained(0));
    params.key_usages = vec![KeyUsagePurpose::KeyCertSign, KeyUsagePurpose::CrlSign];
    let now = OffsetDateTime::now_utc();
    params.not_before = now - Duration::minutes(5);
    params.not_after = now + Duration::days(3650);
    params.serial_number = Some(serial(common_name));
    Ok(params)
}

/// A company's identity certificate: subject O = its approved name, CN = its verified
/// domain, the domain again as a DNS name; for signing documents, not a CA.
pub fn leaf_params(
    organization: &str,
    domain: &str,
    days: i64,
) -> Result<CertificateParams, String> {
    if organization.trim().is_empty() || domain.trim().is_empty() {
        return Err("the organisation and the domain are required".into());
    }
    let mut params = CertificateParams::new(Vec::<String>::new()).map_err(|e| e.to_string())?;
    params.distinguished_name = rcgen::DistinguishedName::new();
    params
        .distinguished_name
        .push(DnType::OrganizationName, organization);
    params.distinguished_name.push(DnType::CommonName, domain);
    params.subject_alt_names = vec![SanType::DnsName(
        domain
            .try_into()
            .map_err(|_| format!("{domain} is not a DNS name"))?,
    )];
    params.is_ca = IsCa::ExplicitNoCa;
    params.key_usages = vec![KeyUsagePurpose::DigitalSignature];
    params.extended_key_usages = vec![
        ExtendedKeyUsagePurpose::EmailProtection,
        ExtendedKeyUsagePurpose::Other(DOCUMENT_SIGNING.to_vec()),
    ];
    params.use_authority_key_identifier_extension = true;
    let now = OffsetDateTime::now_utc();
    params.not_before = now - Duration::minutes(5);
    params.not_after = now + Duration::days(days);
    params.serial_number = Some(serial(domain));
    Ok(params)
}
