namespace Model;

public struct ParticipantsParquet
{
    public string? record_type { get; set; }
    public long? change_time_stamp { get; set; }
    public long? serial_change_number { get; set; }
    public long? nhs_number { get; set; }
    public long? superseded_by_nhs_number { get; set; }
    public string? primary_care_provider { get; set; }
    public string? primary_care_effective_from_date { get; set; }
    public string? current_posting { get; set; }
    public string? current_posting_effective_from_date { get; set; }
    public string? name_prefix { get; set; }
    public string? given_name { get; set; }
    public string? other_given_name { get; set; }
    public string? family_name { get; set; }
    public string? previous_family_name { get; set; }
    public string? date_of_birth { get; set; }
    public long? gender { get; set; }
    public string? address_line_1 { get; set; }
    public string? address_line_2 { get; set; }
    public string? address_line_3 { get; set; }
    public string? address_line_4 { get; set; }
    public string? address_line_5 { get; set; }
    public string? postcode { get; set; }
    public string? paf_key { get; set; }
    public string? address_effective_from_date { get; set; }
    public string? reason_for_removal { get; set; }
    public string? reason_for_removal_effective_from_date { get; set; }
    public string? date_of_death { get; set; }
    public int? death_status { get; set; }
    public string? home_telephone_number { get; set; }
    public string? home_telephone_effective_from_date { get; set; }
    public string? mobile_telephone_number { get; set; }
    public string? mobile_telephone_effective_from_date { get; set; }
    public string? email_address { get; set; }
    public string? email_address_effective_from_date { get; set; }
    public string? preferred_language { get; set; }
    public bool? is_interpreter_required { get; set; }
    public bool? invalid_flag { get; set; }
    public bool? eligibility { get; set; }
}