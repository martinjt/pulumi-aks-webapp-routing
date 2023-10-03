# Basic AKS Cluster using Pulumi with the WebApp Routing Add-on

This is to support the blog post on my blog at https://martinjt.me

## Login to Pulumi backend

```bash
pulumi login azblob://<container-name>?storage_account=<storage-account-name>
```

## Add config

```bash
pulumi config set dnsResourceGroup <resource-group>
pulumi config set dnsZoneName <dns-zone-name>
pulumi config set azure-native:location uksouth
```

## Run it

```shell
pulumi up
```

## Login to the cluster

```shell
export PULUMI_CONFIG_PASSPHRASE=<your-passphrase
eval $(pulumi stack output --shell)
az aks get-credentials -n $clusterName -g $clusterResourceGroup --overwrite-existing
```