# Linux usage — personal fork

This is a usage reference, not an instruction to install anything during this
source conversion. No fork artifact or installation has been runtime-validated.
The [fork build policy](../../README.md#build-and-verification-policy) uses remote
compilation; no local .NET SDK or C# compiler is required by the intended end-user
workflow.

## Requirements and packaging assumptions

- Linux x64 or arm64. Native/system library requirements still apply and have not
  been verified against a built fork artifact.
- `tar` and `unzip` must be available for the current Linux readiness checks.
- Packaging is configured to bundle `resources/7zz`, `libcurl-impersonate.so` and
  `cacert.pem`. Those are build inputs, not artifacts downloaded or verified by
  this conversion.
- The retained AppImage package metadata lists FUSE2. AppImage startup has not
  been tested here.
- Wine is needed to run the Windows game and `ModOrganizer.exe` payloads. The
  installer itself is Linux-only.

Dependency provisioning and downloads require separate review and authorization.
No installation commands are implied by this list.

## Configuration and installation examples

These existing examples are documentation only; they have not been executed as
part of this work. Do not treat them as evidence of a safe or working installation.

1. Create a config:

   ```bash
   ./stalker-gamma config create \
   --anomaly gamma/anomaly \
   --gamma gamma/gamma \
   --cache gamma/cache \
   --download-threads 4
   ```
2. Install Anomaly and GAMMA:

   ```bash
   ./stalker-gamma full-install
   ```

The examples use this directory layout:

```text
.
├── gamma
│   ├── anomaly
│   ├── cache
│   └── gamma
```

## Wine configuration

Use the Wine prefix intended for the game to run `<gamma-directory>/ModOrganizer.exe`
(`gamma/gamma/ModOrganizer.exe` in the layout above). The generated MO2 INI retains
Wine's `Z:` paths and the Anomaly executable entries.

The existing Wine/winetricks guidance names these prefix components:
`d3dcompiler_43`, `d3dcompiler_47`, `d3dx10`, `d3dx11_43`, `d3dx9`, and `vcrun2022`.
They have not been installed or tested during this conversion.

## Build and verification limits

The manual GitHub Actions draft targets Linux x64/arm64 and does not publish
releases or packages. Build-input pinning/checksums and separate scoped consent
are required before running it. No remote build, isolated Wine configuration
regression, native ABI test, installer, game or server execution is claimed here.

`build/update-pkgbuild.sh` remains a dormant manual metadata generator using the
fork's URLs. No AUR publication or available fork release is implied. Generating
output hashes does not authenticate the tools used to build those outputs.
