namespace Common;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Cis2User
{
    [JsonPropertyName("nhsid_useruid")]
    public string NhsidUseruid { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("nhsid_nrbac_roles")]
    public List<NhsidNrbacRole> NhsidNrbacRoles { get; set; }

    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string FamilyName { get; set; }

    [JsonPropertyName("uid")]
    public string Uid { get; set; }

    [JsonPropertyName("sub")]
    public string Sub { get; set; }
}
public class NhsidNrbacRole
{
    [JsonPropertyName("person_orgid")]
    public string PersonOrgid { get; set; }

    [JsonPropertyName("person_roleid")]
    public string PersonRoleid { get; set; }

    [JsonPropertyName("org_code")]
    public string OrgCode { get; set; }

    [JsonPropertyName("role_name")]
    public string RoleName { get; set; }

    [JsonPropertyName("role_code")]
    public string RoleCode { get; set; }

    [JsonPropertyName("workgroups")]
    public List<string> Workgroups { get; set; }

    [JsonPropertyName("workgroups_codes")]
    public List<string> WorkgroupsCodes { get; set; }
}
