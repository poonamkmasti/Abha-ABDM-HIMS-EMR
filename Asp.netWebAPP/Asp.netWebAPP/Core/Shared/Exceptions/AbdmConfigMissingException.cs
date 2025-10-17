namespace Asp.netWebAPP.Core.Shared.Exceptions
{
    public class AbdmConfigMissingException : Exception
    {
        public AbdmConfigMissingException(string message) : base(message) { }
    }
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
    public class AbdmException : Exception
    {
        public AbdmException(string message) : base(message) { }
    }
    public class MultipleOTPRequest: Exception
    {
        public MultipleOTPRequest(string message) : base(message) { }
    }
}
