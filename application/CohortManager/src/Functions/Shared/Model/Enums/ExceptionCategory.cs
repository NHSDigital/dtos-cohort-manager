namespace Model.Enums;

/// <summary>
/// Exception categories for the exception management table
/// </summary>
public enum ExceptionCategory
{
    Non = 0,
    /// <summary>
    /// Used for validation exceptions for 
    /// BS Select validation rules
    /// </summary>
    BSSelect = 1,
    /// <summary>
    /// Used for validation exceptions for 
    /// CaaS validation rules
    /// </summary>
    CaaS = 2,
    /// <summary>
    /// Used for validation exceptions for 
    /// National Back Office validation rules
    /// </summary>
    NBO = 3,
    /// <summary>
    /// Used for file validation exceptions
    /// </summary>
    File = 5,
    /// <summary>
    /// Used when a file with multiple rows
    /// contains a nil return NHS Number* 
    /// </summary>
    /// <remarks>
    /// *A NHS number that signifies that it is a nil return 
    /// file, meaning there are no updates to process from CaaS
    /// </remarks>
    NilReturnFile = 7,
    /// <summary>
    /// Used for raising an exception when a record is deleted
    /// </summary>
    DeleteRecord = 8,
    /// <summary>
    /// Used for exceptions where the file doesn't match the
    /// schema
    /// </summary>
    Schema = 10,
    /// <summary>
    /// Used for logging that a transformation has been executed
    /// </summary>
    TransformExecuted = 11,
    Confusion = 12,
    Superseded = 13
}
