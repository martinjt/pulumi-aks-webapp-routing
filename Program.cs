using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Compute;
using Pulumi.AzureNative.ContainerService.V20230502Preview;
using Pulumi.AzureNative.ContainerService.V20230502Preview.Inputs;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Resources;
using System.Collections.Generic;
using ResourceIdentityType = Pulumi.AzureNative.ContainerService.V20230502Preview.ResourceIdentityType;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Config();
    var dnsResourceGroup = config.Require("dnsResourceGroup");
    var dnsZoneName = config.Require("dnsZoneName");

    var resourceGroup = new ResourceGroup("pulumi-blog-aks");

    var dnsZone = GetZone.Invoke(new GetZoneInvokeArgs{
        ResourceGroupName = dnsResourceGroup,
        ZoneName = dnsZoneName
    });

    var cluster = new ManagedCluster("pulumi-blog-aks", new ManagedClusterArgs
    {
        Sku = new ManagedClusterSKUArgs
        {
            Name = ManagedClusterSKUName.Base,
            Tier = ManagedClusterSKUTier.Free
        },
        DnsPrefix = "pulumi-blog-aks",
        ResourceGroupName = resourceGroup.Name,
        NodeResourceGroup = resourceGroup.Name.Apply(rg => $"{rg}-nodes"),
        Identity = new ManagedClusterIdentityArgs
        {
            Type = ResourceIdentityType.SystemAssigned
        },
        EnableRBAC = true,
        KubernetesVersion = "1.27.1",
        IngressProfile = new ManagedClusterIngressProfileArgs
        {
            WebAppRouting = new ManagedClusterIngressProfileWebAppRoutingArgs
            {
                Enabled = true,
                DnsZoneResourceId = dnsZone.Apply(z => z.Id)
            }
        },
        AgentPoolProfiles = new[]
        {
            new ManagedClusterAgentPoolProfileArgs
            {
                Name = "agentpool",
                Count = 1,
                Mode = AgentPoolMode.System,
                OsType = OSType.Linux,
                Type = AgentPoolType.VirtualMachineScaleSets,
                VmSize = VirtualMachineSizeTypes.Standard_A2_v2.ToString(),
                MaxPods = 110
            }
        }
    });

    const string DnsZoneContributorRoleDefinitionId = "/providers/Microsoft.Authorization/roleDefinitions/befefa01-2a29-4197-83a8-272ff33ce314";

    var roleAssignment = new RoleAssignment("cluster-dns-contributor", new()
    {
        PrincipalId = cluster.IngressProfile.Apply(ip => ip?.WebAppRouting!.Identity.ObjectId!),
        PrincipalType = PrincipalType.ServicePrincipal,
        RoleDefinitionId = DnsZoneContributorRoleDefinitionId,
        Scope = dnsZone.Apply(z => z.Id)
    });

    return new Dictionary<string, object?>
    {
        ["clusterName"] = cluster.Name,
        ["clusterResourceGroup"] = resourceGroup.Name
    };
});