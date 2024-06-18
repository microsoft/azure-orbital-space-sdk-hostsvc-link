# Azure Orbital Space SDK - Hostsvc-Link

[![spacefx-dev-build-publish](https://github.com/microsoft/azure-orbital-space-sdk-core/actions/workflows/devcontainer-feature-build-publish.yml/badge.svg)](https://github.com/microsoft/azure-orbital-space-sdk-core/actions/workflows/devcontainer-feature-build-publish.yml)

Link Service is the Azure Orbital Space SDK's host service to upload and download files to/from the ground. This is used for updating an app's configuration, sending a payload app's output to the ground, or sending a file to another satellite.

Outputs:

| Item                                                            | Description                                                                      |
| --------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `Microsoft.Azure.SpaceFx.HostServices.Link.Plugins.1.0.0.nupkg` | DotNet Nuget Package for building Hostsvc-Link Plugins                           |
| `Link.proto`                                                    | A protobuf file for serializing and deserializing messages used by Hostsvc-Link. |
| `hostsvc-link:0.11.0`                                           | Container image for app                                                          |
| `hostsvc-link:0.11.0_base`                                      | Base container image for app.  Requires SpaceSDK_Base and build service          |

## Building

1. Provision /var/spacedev

    ```bash
    # clone the azure-orbital-space-sdk-setup repo and provision /var/spacedev
    git clone https://github.com/microsoft/azure-orbital-space-sdk-setup
    cd azure-orbital-space-sdk-setup
    bash ./.vscode/copy_to_spacedev.sh
    cd -
    ```

1. Build the nuget packages and the container images.  (Note: container images will automatically push)

    ```bash
    # clone this repo
    git clone https://github.com/microsoft/azure-orbital-space-sdk-hostsvc-link

    cd azure-orbital-space-sdk-hostsvc-link

    # Trigger the build_app.sh from azure-orbital-space-sdk-setup
    /var/spacedev/build/dotnet/build_app.sh \
        --repo-dir ${PWD} \
        --app-project src/hostsvc-link.csproj \
        --nuget-project src_pluginBase/pluginBase.csproj \
        --architecture amd64 \
        --output-dir /var/spacedev/tmp/hostsvc-link \
        --app-version 0.11.0 \
        --annotation-config azure-orbital-space-sdk-hostsvc-link.yaml
    ```

1. Copy the build artifacts to their locations in /var/spacedev

    ```bash
    sudo mkdir -p /var/spacedev/nuget/link
    sudo mkdir -p /var/spacedev/protos/spacefx/protos/link

    sudo cp /var/spacedev/tmp/hostsvc-link/amd64/nuget/Microsoft.Azure.SpaceSDK.HostServices.Link.Plugins.0.11.0.nupkg /var/spacedev/nuget/link/
    sudo cp ${PWD}/src/Protos/Link.proto /var/spacedev/protos/spacefx/protos/link/
    ```

1. Push the artifacts to the container registry

    ```bash
    # Push the nuget package to the container registry
    /var/spacedev/build/push_build_artifact.sh \
            --artifact /var/spacedev/nuget/link/Microsoft.Azure.SpaceSDK.HostServices.Link.Plugins.0.11.0.nupkg \
            --annotation-config azure-orbital-space-sdk-hostsvc-link.yaml \
            --architecture amd64 \
            --artifact-version 0.11.0

    # Push the proto to the container registry
    /var/spacedev/build/push_build_artifact.sh \
            --artifact /var/spacedev/protos/spacefx/protos/link/Link.proto \
            --annotation-config azure-orbital-space-sdk-hostsvc-link.yaml \
            --architecture amd64 \
            --artifact-version 0.11.0
    ```

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
