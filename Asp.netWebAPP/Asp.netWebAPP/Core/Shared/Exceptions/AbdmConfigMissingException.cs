namespace Asp.netWebAPP.Core.Shared.Exceptions
{
    public class AbdmUserNotFoundException : Exception
    {
        public AbdmUserNotFoundException(string message) : base(message) { }
    }
    public class InvalidOtpException : Exception
    {
        public InvalidOtpException(string message) : base(message) { }
    }
    public class TooManyRequest : Exception
    {
        public TooManyRequest(string message) : base(message) { }
    }
    public class InvalidAadhaarException : Exception
    {
        public InvalidAadhaarException(string message) : base(message) { }
    }

    public class BaseAbdmException : Exception
    {
        public BaseAbdmException(string message) : base(message) { }
        public BaseAbdmException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmConfigMissingException : BaseAbdmException
    {
        public AbdmConfigMissingException(string message) : base(message) { }
        public AbdmConfigMissingException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmException : BaseAbdmException
    {
        public AbdmException(string message) : base(message) { }
        public AbdmException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmPatientNotFoundException : BaseAbdmException
    {
        public AbdmPatientNotFoundException(string message) : base(message) { }
        public AbdmPatientNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmPatientDataIncompleteException : BaseAbdmException
    {
        public AbdmPatientDataIncompleteException(string message) : base(message) { }
        public AbdmPatientDataIncompleteException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmLinkTokenGenerationException : BaseAbdmException
    {
        public AbdmLinkTokenGenerationException(string message) : base(message) { }
        public AbdmLinkTokenGenerationException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmLinkTokenExpiredException : BaseAbdmException
    {
        public AbdmLinkTokenExpiredException(string message) : base(message) { }
        public AbdmLinkTokenExpiredException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmExternalApiException : BaseAbdmException
    {
        public AbdmExternalApiException(string message) : base(message) { }
        public AbdmExternalApiException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmInvalidCareContextRequestException : BaseAbdmException
    {
        public AbdmInvalidCareContextRequestException(string message) : base(message) { }
        public AbdmInvalidCareContextRequestException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class AbdmM2Exception : BaseAbdmException
    {
        public AbdmM2Exception(string message) : base(message) { }
        public AbdmM2Exception(string message, Exception innerException) : base(message, innerException) { }
    }
    public class DatabaseAccessException : BaseAbdmException
    {
        public DatabaseAccessException(string message) : base(message) { }
        public DatabaseAccessException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class MultipleOTPRequest: Exception
    {
        public MultipleOTPRequest(string message) : base(message) { }
    }
}
