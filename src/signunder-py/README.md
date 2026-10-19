# signunder

Python client for [SignUnder](https://signunder.com): sign the images your product generates under your own company's name, with C2PA Content Credentials, an invisible watermark and a fingerprint on record, and verify them later, even after a platform strips the metadata.

```sh
pip install signunder
```

Get an API key at [signunder.com/dashboard](https://signunder.com/dashboard) and set it as `SIGNUNDER_API_KEY`.

## Sign

```python
from signunder import Signunder

with Signunder() as client:  # reads SIGNUNDER_API_KEY
    signed = client.sign("picture.png")  # a path, bytes or an open file: JPEG, PNG or WebP
    signed.save("picture.signed.png")
    print(signed.record_id)
```

Serve the signed bytes unchanged: re-encoding the image breaks the signature, though the watermark and fingerprint still identify it on your public verify page, `https://signunder.com/verify?t=<your-company>&id=<record_id>`.

## Verify and look up

```python
result = client.verify("downloaded.png")
if result.manifest_found:
    print(result.signed_by, result.validation_state, "altered" if result.altered else "intact")

record = client.get_record(signed.record_id)  # None if it is not one of yours
```

## Async

```python
from signunder import AsyncSignunder

async with AsyncSignunder() as client:
    signed = await client.sign(image_bytes)
```

## Errors

Every refusal raises `SignunderError` with the HTTP `status_code` and the API's `detail`. A failure on SignUnder's side carries a `reference` to quote to support. A month's allowance used up is a 429 whose detail says when it resets.

Early access runs on SignUnder's staging service and signs with a test certificate until your company is listed with the C2PA. Full API reference: [signunder.com/docs](https://signunder.com/docs).
