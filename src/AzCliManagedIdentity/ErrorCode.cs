namespace AzCliManagedIdentity;

public enum ErrorCode
{
    /// <summary>
    /// No request error (can be used to indicate Not Found)
    /// </summary>
    None,
    
    /// <summary>
    /// Generic bad request
    /// </summary>
    /// <remarks>
    /// <c>{"error":{"code":"invalid_request","message":"Invalid request"}}</c>
    /// </remarks>
    BadRequest,
    
    /// <summary>
    /// <c>Metadata:true</c> header not specified
    /// </summary>
    /// <remarks>
    /// <c>{"error":{"code":"bad_request_102","message":"Required metadata header not specified"}}</c>
    /// </remarks>
    MetadataHeaderMissing,
    
    /// <summary>
    /// Required resource parameter not specified
    /// </summary>
    /// <remarks>
    /// <c>{"error":{"code":"invalid_request","message":"Required audience parameter not specified"}}</c>
    /// </remarks>
    ResourceNotSpecified,
    
    /// <summary>
    /// Azure CLI credential unavailable
    /// </summary>
    CredentialUnavailable,
}