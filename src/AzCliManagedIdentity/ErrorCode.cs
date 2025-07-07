namespace AzCliManagedIdentity;

public enum ErrorCode
{
    /// <summary>
    /// Generic bad request
    /// </summary>
    /// <remarks>
    /// <c>{"error":{"code":"invalid_request","message":"Invalid request"}}</c>
    /// </remarks>
    BadRequest = 1,
    
    /// <summary>
    /// <c>Metadata:true</c> header not specified
    /// </summary>
    /// <remarks>
    /// <c>{"error":{"code":"bad_request_102","message":"Required metadata header not specified"}}</c>
    /// </remarks>
    MetadataHeaderMissing = 2,
    
    /// <summary>
    /// Required resource parameter not specified
    /// </summary>
    /// <remarks>
    /// <c>{"error":{"code":"invalid_request","message":"Required audience parameter not specified"}}</c>
    /// </remarks>
    ResourceNotSpecified  = 3
}