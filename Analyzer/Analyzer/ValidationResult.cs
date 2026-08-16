using System;
using System.Collections.Generic;
using System.Text;

namespace Analyzer
{
    public class ValidationResult
    {

        public bool IsValid
        {
            get;
        }

        public string TrimmedUrl
        {
            get;
        }

        public string Message
        {
            get;
        }

        public ValidationResult(bool isValid, string trimmedUrl, string message)
        {
            IsValid = isValid;
            TrimmedUrl = trimmedUrl;
            Message = message;
        }

        public override string ToString()
        {
            return $"{IsValid} - {TrimmedUrl} - {Message}";
        }
    }
}
