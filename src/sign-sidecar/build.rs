fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_prost_build::configure()
        .build_client(false)
        .compile_protos(
            &["../../proto/signunder/sign/v1/sign.proto"],
            &["../../proto"],
        )?;
    Ok(())
}
