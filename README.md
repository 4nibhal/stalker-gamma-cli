# stalker-gamma-cli — Linux-only personal fork

Personal fork: https://github.com/4nibhal/stalker-gamma-cli

Derived from https://github.com/FaithBeam/stalker-gamma-cli; the existing
[GPLv3 license](LICENSE) is retained.

_stalker-gamma-cli_ is a CLI to install Stalker Anomaly and the GAMMA mod pack.
This fork targets Linux x64 and arm64. Game and Mod Organizer executables remain
Windows payloads run with Wine; they are not Windows host support for the installer.

## Build and verification policy

No local .NET SDK or C# compiler is required by this fork's intended end-user
workflow. The CLI is configured for NativeAOT; the optional companion server
bundles its .NET runtime without AOT. Native/system library requirements still
apply.

Bounded remote verification uses manual `workflow_dispatch` only, testing source
`71e9c8839064cb56e4761f04c1cfb9e8ba8ef642` with exactly preinstalled SDK
`10.0.401` (no roll-forward or prereleases) on native `ubuntu-22.04`/`linux-x64`
and `ubuntu-22.04-arm`/`linux-arm64`. Missing prerequisites stop the job; no SDK
or system packages are installed. Inline Git retrieves that public source, not
the workflow revision; both revisions are logged separately. Payloads stay under
`/tmp/stalker-gamma-ci/`.
The scope is source checks, six regression suites, CLI/server publication and ELF
inspection without launching either application, packaging, releases or uploads.

Public branch/logs, mutable hosted images and an initial unresolved NuGet.org-only
restore are explicitly accepted exceptions, not verified immutable inputs.
Generated lock identities, resolved versions and content hashes are observations,
not trusted proof. Preparing this workflow is not evidence of an executed build.
Local checks use `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -v`;
source contracts and Make dry runs do not replace compilation, runtime tests or
downloaded-content verification. No validated fork release is claimed here.

See [Linux usage](build/linux/README.md) for the installation layout and Wine
configuration. The platform conversion does not complete the staged filesystem,
download, permissions, subprocess, optional-server or supply-chain security work.
The converted application is not yet presented as ready for personal installation.

## Commands

The existing command reference is retained below; it has not been runtime-verified
as part of this source conversion.

```
❯ stalker-gamma
Usage: [command] [-h|--help] [--version]

Commands:
  anomaly-install                    Installs Stalker Anomaly.
  check-anomaly                      Verifies the integrity of Stalker Anomaly
  config                             Get the currently active profile
  config create                      Create settings file
  config delete                      Delete a profile. If this profile was active, you should set another to be active with config use
  config list                        List profiles
  config use                         Set a profile as active.
  debug hash-install                 For debugging broken installations only. Hashes installation folders and creates a compressed archive containing the computed hashes.
  full-install                       This will install/update Anomaly and all GAMMA addons.
  mo2 config get selected-profile    Retrieves the selected profile information from the ModOrganizer.ini file within the specified directory.
  mo2 config set selected-profile    Updates the selected profile in the ModOrganizer.ini file for the specified directory.
  mo2 mod delete                     Deletes a specified mod in the provided profile.
  mo2 mod disable                    Disables a specified mod in the given profile
  mo2 mod enable                     Enables a specified mod within a given profile.
  mo2 mod status                     Show mod status info
  mo2 profile delete                 Deletes a specified profile from a Gamma installation.
  mo2 profile list mods              Lists all mods in a profile.
  mo2 profiles list                  Lists all profiles in a gamma installation.
  update apply                       Apply any updates
  update check                       Check for updates
```

## Features

- Install Stalker Anomaly
- Install GAMMA mod pack
- Manage mod lists for GAMMA
- Update support

## Usage

### Install

The [Linux usage reference](build/linux/README.md) preserves the configuration and
installation examples. It is documentation, not authorization to download content
or run the installer during this work.

### Update

After you've performed a full-install, you can update your installation.

Existing update usage (not executed or validated in this conversion):

```bash
stalker-gamma update check
stalker-gamma update apply
```

## Troubleshooting

[Upstream troubleshooting wiki](https://github.com/FaithBeam/stalker-gamma-cli/wiki/Troubleshooting).
Upstream documentation is background material, not a verified setup guide for this fork.

## Advanced Options

[Upstream advanced options](https://github.com/FaithBeam/stalker-gamma-cli/wiki/Advanced).
Refer to the build and verification policy above rather than installing a local SDK.
