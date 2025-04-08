namespace Model.Enums;

/// <summary>
/// HL7 Confidentiality levels based on the HL7 v3-Confidentiality CodeSystem
/// http://terminology.hl7.org/CodeSystem/v3-Confidentiality
/// </summary>
public enum ConfidentialityLevel
{
    /// <summary>
    /// No specific confidentiality level found in the record
    /// </summary>
    NotSpecified = 0,

    /// <summary>
    /// Unrestricted (U): No level of protection is required to safeguard personal and healthcare 
    /// information that has been disclosed by an authorized individual without restrictions on its use.
    /// Example: Includes publicly available information e.g., business name, phone, email and physical address.
    /// </summary>
    Unrestricted = 1,

    /// <summary>
    /// Low (L): Low level of protection is required to safeguard personal and healthcare information,
    /// which has been altered in such a way as to minimize the need for confidentiality protections with some 
    /// residual risks associated with re-linking.
    /// Example: Personal and healthcare information in a HIPAA Limited Data Set.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Moderate (M): Level of protection required to safeguard personal and healthcare information, 
    /// which if disclosed without authorization, would present a moderate risk of harm to an individual's 
    /// reputation and sense of privacy.
    /// Example: Information an individual authorizes to be collected for personal health records, consumer devices, etc.
    /// </summary>
    Moderate = 3,

    /// <summary>
    /// Normal (N): Level of protection required to safeguard personal and healthcare information, 
    /// which if disclosed without authorization, would present a considerable risk of harm to an individual's 
    /// reputation and sense of privacy.
    /// Example: In the US, includes what HIPAA identifies as protected health information (PHI).
    /// </summary>
    Normal = 4,

    /// <summary>
    /// Restricted (R): Level of protection required to safeguard potentially stigmatizing information, 
    /// which if disclosed without authorization, would present a high risk of harm to an individual's 
    /// reputation and sense of privacy.
    /// Example: Information about sensitive conditions (mental health, HIV, substance abuse), celebrities, employees, etc.
    /// </summary>
    Restricted = 5,

    /// <summary>
    /// Very Restricted (V): Highest level of protection for extremely sensitive information.
    /// </summary>
    VeryRestricted = 6
}
