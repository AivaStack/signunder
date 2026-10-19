//! The sign sidecar's signing, validation and transport code, shared by the gRPC server
//! (`main.rs`) and the `kms-csr` tool.

pub mod identity;
pub mod identity_ca;
pub mod internal_tls;
pub mod kms;
pub mod kms_key;
pub mod provenance;
